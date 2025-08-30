using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ChatApp.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBroker? _messageBroker;
    private readonly UserManager<ApplicationUser> _userManager;
    
    // Static dictionary to track online users across all hub instances
    private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

    public ChatHub(
        IServiceProvider serviceProvider,
        UserManager<ApplicationUser> userManager, 
        IMessageBroker? messageBroker = null)
    {
        _serviceProvider = serviceProvider;
        _messageBroker = messageBroker;
        _userManager = userManager;
    }

    public async Task SendMessage(string message)
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName == null) return;

        // Check if it's a stock command
        if (message.StartsWith("/stock=", StringComparison.OrdinalIgnoreCase))
        {
            var stockCode = message.Substring(7).Trim();
            if (!string.IsNullOrEmpty(stockCode) && _messageBroker != null)
            {
                try
                {
                    await _messageBroker.PublishStockCommandAsync(stockCode, user.UserName);
                    
                    // Acknowledge the command to the user
                    await Clients.Caller.SendAsync("ReceiveMessage", "System", 
                        $"📈 Looking up stock quote for {stockCode.ToUpper()}...", DateTime.UtcNow);
                }
                catch (Exception)
                {
                    // Silently handle RabbitMQ connection issues
                    await Clients.Caller.SendAsync("ReceiveMessage", "System", 
                        "Stock service is currently unavailable.", DateTime.UtcNow);
                }
            }
            else if (_messageBroker == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", 
                    "Stock service is not configured.", DateTime.UtcNow);
            }
            return;
        }

        // Regular chat message - use scoped repository
        using var scope = _serviceProvider.CreateScope();
        var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
        
        var chatMessage = new ChatMessage
        {
            Content = message,
            Username = user.UserName,
            UserId = user.Id,
            Timestamp = DateTime.UtcNow,
            IsStockQuote = false
        };

        await chatRepository.AddMessageAsync(chatMessage);
        await Clients.All.SendAsync("ReceiveMessage", user.UserName, message, chatMessage.Timestamp);
    }
    
    public async Task JoinChat()
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName == null) return;

        // Add to ChatRoom group
        await Groups.AddToGroupAsync(Context.ConnectionId, "ChatRoom");
        
        // Add user to online users list
        OnlineUsers.TryAdd(Context.ConnectionId, user.UserName);
        
        // Update user status in database
        user.IsOnline = true;
        await _userManager.UpdateAsync(user);
        
        // Notify all clients
        await Clients.Group("ChatRoom").SendAsync("UserJoined", user.UserName);
        
        // Send updated online users list to all clients
        await Clients.Group("ChatRoom").SendAsync("UpdateOnlineUsers", GetOnlineUsersList());
    }
    
    public async Task UserTyping()
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName == null) return;

        // Notify all other users (not the sender) that this user is typing
        await Clients.Others.SendAsync("UserTyping", user.UserName);
    }

    public async Task UserStoppedTyping()
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName == null) return;

        // Notify all other users that this user stopped typing
        await Clients.Others.SendAsync("UserStoppedTyping", user.UserName);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        await JoinChat();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove user from online users list
        if (OnlineUsers.TryRemove(Context.ConnectionId, out var username))
        {
            // Check if user has other connections
            var hasOtherConnections = OnlineUsers.Values.Contains(username);
            
            if (!hasOtherConnections)
            {
                // Update user status in database only if no other connections
                var user = await _userManager.FindByNameAsync(username);
                if (user != null)
                {
                    user.IsOnline = false;
                    await _userManager.UpdateAsync(user);
                }
                
                // Notify all clients that user left
                await Clients.Group("ChatRoom").SendAsync("UserLeft", username);
            }
            
            // Send updated online users list
            await Clients.Group("ChatRoom").SendAsync("UpdateOnlineUsers", GetOnlineUsersList());
        }

        await base.OnDisconnectedAsync(exception);
    }
    
    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return null;
            
        return await _userManager.GetUserAsync(Context.User);
    }
    
    private static List<string> GetOnlineUsersList()
    {
        return OnlineUsers.Values.Distinct().ToList();
    }
}