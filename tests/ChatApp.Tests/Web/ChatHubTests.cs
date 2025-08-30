using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using ChatApp.Web.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace ChatApp.Tests.Web;

public class ChatHubTests
{
    private static (ChatHub hub, Mock<IChatRepository> repo, Mock<IMessageBroker> broker, ApplicationUser user, 
        Mock<IHubCallerClients> clients, Mock<ISingleClientProxy> caller, Mock<IClientProxy> all, Mock<IGroupManager> groups)
        CreateHub()
    {
        var repo = new Mock<IChatRepository>();
        var broker = new Mock<IMessageBroker>();
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userMgr = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);

        var user = new ApplicationUser { Id = "u1", UserName = "john" };
        userMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userMgr.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        
        var scope = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(s => s.GetService(typeof(IChatRepository))).Returns(repo.Object);
        scope.SetupGet(s => s.ServiceProvider).Returns(spMock.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var root = new ServiceCollection().AddSingleton(scopeFactory.Object).BuildServiceProvider();
        var rootProvider = new PassthroughProvider(root, scopeFactory.Object);

        var hub = new ChatHub(rootProvider, userMgr.Object, broker.Object);
        var context = new HubCallerContextMock("conn-1", principal: MakePrincipal("john"));
        var clients = new Mock<IHubCallerClients>();
        var caller = new Mock<ISingleClientProxy>();
        var all = new Mock<IClientProxy>();
        var groups = new Mock<IGroupManager>();
        clients.SetupGet(c => c.Caller).Returns(caller.Object);
        clients.SetupGet(c => c.All).Returns(all.Object);

        hub.Context = context;
        hub.Clients = clients.Object;
        hub.Groups = groups.Object;

        return (hub, repo, broker, user, clients, caller, all, groups);
    }

    private sealed class PassthroughProvider : IServiceProvider
    {
        private readonly IServiceProvider _root;
        private readonly IServiceScopeFactory _factory;
        public PassthroughProvider(IServiceProvider root, IServiceScopeFactory factory) { _root = root; _factory = factory; }
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory)) return _factory;
            return _root.GetService(serviceType);
        }
    }

    private static ClaimsPrincipal MakePrincipal(string username)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private sealed class HubCallerContextMock : HubCallerContext
    {
        private readonly string _connectionId;
        private readonly ClaimsPrincipal _user;
        private readonly IDictionary<object, object?> _items = new Dictionary<object, object?>();
        private readonly Microsoft.AspNetCore.Http.Features.FeatureCollection _features = new();
        public HubCallerContextMock(string connectionId, ClaimsPrincipal principal)
        { _connectionId = connectionId; _user = principal; }
        public override string ConnectionId => _connectionId;
        public override ClaimsPrincipal User => _user;
        public override string? UserIdentifier { get; } = null;
        public override IDictionary<object, object?> Items => (IDictionary<object, object?>)_items;
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features => _features;
        public override CancellationToken ConnectionAborted { get; } = CancellationToken.None;
        public override void Abort() { }
    }

    [Fact(DisplayName = "ChatHub: routes /stock= to broker and acknowledges"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task Routes_Stock_Command()
    {
        var (hub, repo, broker, user, clients, caller, all, groups) = CreateHub();
        broker.Setup(b => b.PublishStockCommandAsync("aapl", "john")).Returns(Task.CompletedTask);
        caller.Setup(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);

        await hub.SendMessage("/stock=aapl");

        broker.Verify(b => b.PublishStockCommandAsync("aapl", "john"), Times.Once);
        caller.Verify(p => p.SendCoreAsync(
            "ReceiveMessage",
            It.Is<object?[]>(args => args.Length == 3 && (string)args[0] == "System"),
            default), Times.Once);
    }

    [Fact(DisplayName = "ChatHub: regular message saved and broadcasted"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task Saves_And_Broadcasts_Message()
    {
        var (hub, repo, broker, user, clients, caller, all, groups) = CreateHub();
        repo.Setup(r => r.AddMessageAsync(It.IsAny<ChatMessage>())).ReturnsAsync((ChatMessage m) => m);
        all.Setup(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);

        await hub.SendMessage("hello world");

        repo.Verify(r => r.AddMessageAsync(It.Is<ChatMessage>(m => m.Content == "hello world" && m.UserId == "u1")), Times.Once);
        all.Verify(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), default), Times.Once);
    }

    [Fact(DisplayName = "ChatHub: JoinChat adds to group and updates status"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task JoinChat_Adds_To_Group_Updates_Status()
    {
        var (hub, repo, broker, user, clients, caller, all, groups) = CreateHub();
        groups.Setup(g => g.AddToGroupAsync("conn-1", "ChatRoom", default)).Returns(Task.CompletedTask);
        all.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);
        var groupProxy = new Moq.Mock<IClientProxy>();
        groupProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);
        clients.Setup(c => c.Group("ChatRoom")).Returns(groupProxy.Object);

        await hub.JoinChat();

        groups.Verify(g => g.AddToGroupAsync("conn-1", "ChatRoom", default), Times.Once);
    }

    private static (ChatHub hub, Mock<IHubCallerClients> clients, Mock<ISingleClientProxy> caller, Mock<IClientProxy> all, 
        Mock<IGroupManager> groups) CreateHubWithUser(string username, IMessageBroker? broker)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userMgr = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
        userMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new ApplicationUser { Id = "u1", UserName = username });
        userMgr.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var scope = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(s => s.GetService(typeof(IChatRepository))).Returns(new Mock<IChatRepository>().Object);
        scope.SetupGet(s => s.ServiceProvider).Returns(spMock.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var root = new ServiceCollection().AddSingleton(scopeFactory.Object).BuildServiceProvider();
        var rootProvider = new PassthroughProvider(root, scopeFactory.Object);

        var hub = new ChatHub(rootProvider, userMgr.Object, broker);
        var context = new HubCallerContextMock("c1", MakePrincipal(username));
        var clients = new Mock<IHubCallerClients>();
        var caller = new Mock<ISingleClientProxy>();
        var all = new Mock<IClientProxy>();
        var groups = new Mock<IGroupManager>();
        clients.SetupGet(c => c.Caller).Returns(caller.Object);
        clients.SetupGet(c => c.All).Returns(all.Object);

        hub.Context = context;
        hub.Clients = clients.Object;
        hub.Groups = groups.Object;

        return (hub, clients, caller, all, groups);
    }

    [Fact(DisplayName = "ChatHub: broker null => not configured message"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task Broker_Null_Sends_NotConfigured_Message()
    {
        var (hub, _, caller, _, _) = CreateHubWithUser("john", null);
        caller.Setup(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);

        await hub.SendMessage("/stock=aapl");

        caller.Verify(p => p.SendCoreAsync(
            "ReceiveMessage",
            It.Is<object?[]>(args => args.Length == 3 && args[0] != null && args[0] is string && (string)args[0] == "System" && args[1] != null && args[1] is string && ((string)args[1]).Contains("not configured")),
            default), Times.Once);
    }

    [Fact(DisplayName = "ChatHub: broker throws => service unavailable"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task Broker_Throws_Sends_Service_Unavailable()
    {
        var broker = new Mock<IMessageBroker>();
        broker.Setup(b => b.PublishStockCommandAsync("aapl", "john")).ThrowsAsync(new Exception("boom"));
        var (hub, _, caller, _, _) = CreateHubWithUser("john", broker.Object);
        caller.Setup(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);

        await hub.SendMessage("/stock=aapl");

        caller.Verify(p => p.SendCoreAsync(
            "ReceiveMessage",
            It.Is<object?[]>(args => args.Length == 3 && args[0] != null && args[0] is string && (string)args[0] == "System" && args[1] != null && args[1] is string && ((string)args[1]).Contains("unavailable")),
            default), Times.Once);
    }

    [Fact(DisplayName = "ChatHub: OnDisconnected updates status when last connection"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task OnDisconnected_LastConnection_Updates_Status()
    {
        var username = $"user-{Guid.NewGuid():N}";
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userMgr = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
        var user = new ApplicationUser { Id = "u1", UserName = username };
        userMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userMgr.Setup(m => m.FindByNameAsync(username)).ReturnsAsync(user);
        userMgr.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var scope = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(s => s.GetService(typeof(IChatRepository))).Returns(new Mock<IChatRepository>().Object);
        scope.SetupGet(s => s.ServiceProvider).Returns(spMock.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var root = new ServiceCollection().AddSingleton(scopeFactory.Object).BuildServiceProvider();
        var provider = new PassthroughProvider(root, scopeFactory.Object);

        var hub = new ChatHub(provider, userMgr.Object, new Mock<IMessageBroker>().Object);
        var connId = $"conn-{Guid.NewGuid():N}";
        var context = new HubCallerContextMock(connId, MakePrincipal(username));
        var clients = new Mock<IHubCallerClients>();
        var groupProxy = new Mock<IClientProxy>();
        clients.Setup(c => c.Group("ChatRoom")).Returns(groupProxy.Object);
        hub.Clients = clients.Object;
        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.AddToGroupAsync(connId, "ChatRoom", default)).Returns(Task.CompletedTask);
        hub.Groups = groups.Object;
        hub.Context = context;

        // Simulate user joining to populate static OnlineUsers
        await hub.JoinChat();
        await hub.OnDisconnectedAsync(null);

        userMgr.Verify(m => m.UpdateAsync(It.Is<ApplicationUser>(u => u.IsOnline == false)), Times.AtLeastOnce);
        groupProxy.Verify(p => p.SendCoreAsync("UserLeft", It.Is<object?[]>(args => (string)args[0] == username), default), Times.Once);
    }

    [Fact(DisplayName = "ChatHub: OnConnected triggers JoinChat"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task OnConnected_Triggers_JoinChat()
    {
        var (hub, _, _, all, groups) = CreateHubWithUser("john", new Mock<IMessageBroker>().Object);
        groups.Setup(g => g.AddToGroupAsync("c1", "ChatRoom", default)).Returns(Task.CompletedTask);
        all.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);
        var groupProxy = new Mock<IClientProxy>();
        groupProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default)).Returns(Task.CompletedTask);
        var clients = Mock.Get(hub.Clients);
        clients.Setup(c => c.Group("ChatRoom")).Returns(groupProxy.Object);

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync("c1", "ChatRoom", default), Times.Once);
    }
}
