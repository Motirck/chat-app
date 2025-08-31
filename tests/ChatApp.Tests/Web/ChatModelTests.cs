using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using ChatApp.Web.Pages;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChatApp.Tests.Web;

public class ChatModelTests
{
    [Fact(DisplayName = "ChatModel: OnGet loads user and messages"), Trait("Category","Unit"), Trait("Area","Web")]
    public async Task OnGet_Loads_User_And_Messages()
    {
        var repo = new Mock<IChatRepository>();
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userMgr = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
        var logger = NullLogger<ChatModel>.Instance;

        var user = new ApplicationUser { Id = "u1", UserName = "john" };
        userMgr.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        userMgr.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var messages = new List<ChatMessage> { new ChatMessage { Content = "hi", Timestamp = DateTime.UtcNow, UserId = "u1", Username = "john" } };
        repo.Setup(r => r.GetLastMessagesAsync(50, It.IsAny<string?>())).ReturnsAsync(messages);
        repo.Setup(r => r.GetAvailableRoomsAsync()).ReturnsAsync(new List<ChatRoom>{ new ChatRoom{ Id = "lobby", Name = "Lobby", CreatedAt = DateTime.UtcNow } });

        var model = new ChatModel(repo.Object, userMgr.Object, logger);
        await model.OnGetAsync(null);

        model.CurrentUser.Should().NotBeNull();
        model.RecentMessages.Should().HaveCount(1);
        user.IsOnline.Should().BeTrue();
    }
}
