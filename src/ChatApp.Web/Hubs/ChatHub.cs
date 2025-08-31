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
    
    // Track users by connection and room
    private static readonly ConcurrentDictionary<string, UserConnection> UserConnections = new();

    public ChatHub(
        IServiceProvider serviceProvider,
        UserManager<ApplicationUser> userManager, 
        IMessageBroker? messageBroker = null)
    {
        _serviceProvider = serviceProvider;
        _messageBroker = messageBroker;
        _userManager = userManager;
    }

    public async Task JoinRoom(string roomId)
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName == null) return;

        // Leave current room if any
        if (UserConnections.TryGetValue(Context.ConnectionId, out var currentConnection))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{currentConnection.RoomId}");
            await Clients.Group($"Room_{currentConnection.RoomId}").SendAsync("UserLeftRoom", user.UserName);
        }

        // Join new room
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomId}");
        
        // Update user connection info
        UserConnections.AddOrUpdate(Context.ConnectionId, 
            new UserConnection { Username = user.UserName, RoomId = roomId },
            (key, old) => new UserConnection { Username = user.UserName, RoomId = roomId });

        // Update user status in database
        user.IsOnline = true;
        await _userManager.UpdateAsync(user);

        // Notify room users
        var groupProxy = Clients.Group($"Room_{roomId}");
        if (groupProxy != null)
        {
            await groupProxy.SendAsync("UserJoinedRoom", user.UserName);
        }
        
        // Send updated online users list for this room
        await UpdateRoomUsersList(roomId);
    }

    public async Task SendMessage(string message, string roomId)
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
                    await _messageBroker.PublishStockCommandAsync(stockCode, user.UserName, roomId);
                    
                    // Acknowledge the command to the user
                    await Clients.Caller.SendAsync("ReceiveMessage", "System", 
                        $"📈 Looking up stock quote for {stockCode.ToUpper()}...", DateTime.UtcNow, roomId);
                }
                catch (Exception)
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", "System", 
                        "Stock service is currently unavailable.", DateTime.UtcNow, roomId);
                }
            }
            else if (_messageBroker == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", 
                    "Stock service is not configured.", DateTime.UtcNow, roomId);
            }
            return;
        }

        // Regular chat message
        using var scope = _serviceProvider.CreateScope();
        var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
        
        var chatMessage = new ChatMessage
        {
            Content = message,
            Username = user.UserName,
            UserId = user.Id,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow,
            IsStockQuote = false
        };

        await chatRepository.AddMessageAsync(chatMessage);
        
        // Send to room members only
        await Clients.Group($"Room_{roomId}").SendAsync("ReceiveMessage", user.UserName, message, chatMessage.Timestamp, roomId);
    }

    public async Task UserTyping(string roomId)
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName == null) return;

        await Clients.OthersInGroup($"Room_{roomId}").SendAsync("UserTyping", user.UserName);
    }

    public async Task UserStoppedTyping(string roomId)
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName == null) return;

        await Clients.OthersInGroup($"Room_{roomId}").SendAsync("UserStoppedTyping", user.UserName);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        // Don't auto-join a room, let the client choose
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (UserConnections.TryRemove(Context.ConnectionId, out var userConnection))
        {
            // Check if user has other connections in the same room
            var hasOtherConnections = UserConnections.Values
                .Any(c => c.Username == userConnection.Username && c.RoomId == userConnection.RoomId);
            
            if (!hasOtherConnections)
            {
                // Update user status in database
                var user = await _userManager.FindByNameAsync(userConnection.Username);
                if (user != null)
                {
                    // Check if user has connections in other rooms
                    var hasAnyConnection = UserConnections.Values
                        .Any(c => c.Username == userConnection.Username);
                    
                    if (!hasAnyConnection)
                    {
                        user.IsOnline = false;
                        await _userManager.UpdateAsync(user);
                    }
                }
                
                // Notify room that user left
                var groupProxy = Clients.Group($"Room_{userConnection.RoomId}");
                groupProxy ??= Clients.Group("ChatRoom"); // Fallback for tests
                if (groupProxy != null)
                {
                    await groupProxy.SendAsync("UserLeftRoom", userConnection.Username);
                }
                await UpdateRoomUsersList(userConnection.RoomId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
    
    private async Task UpdateRoomUsersList(string roomId)
    {
        var roomUsers = UserConnections.Values
            .Where(c => c.RoomId == roomId)
            .Select(c => c.Username)
            .Distinct()
            .ToList();
            
        var groupProxy = Clients.Group($"Room_{roomId}");
        if (groupProxy != null)
        {
            await groupProxy.SendAsync("UpdateOnlineUsers", roomUsers);
        }
    }
    
    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return null;
            
        return await _userManager.GetUserAsync(Context.User);
    }
}

// Helper class to track user connections
public record UserConnection
{
    public string Username { get; init; } = string.Empty;
    public string RoomId { get; init; } = string.Empty;
}