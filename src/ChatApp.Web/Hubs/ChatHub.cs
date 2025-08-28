using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageBroker _messageBroker;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatHub(IChatRepository chatRepository, IMessageBroker messageBroker, UserManager<ApplicationUser> userManager)
    {
        _chatRepository = chatRepository;
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
            if (!string.IsNullOrEmpty(stockCode))
            {
                await _messageBroker.PublishStockCommandAsync(stockCode, user.UserName);
            }
            return;
        }

        // Regular chat message
        var chatMessage = new ChatMessage
        {
            Content = message,
            Username = user.UserName,
            UserId = user.Id,
            Timestamp = DateTime.UtcNow,
            IsStockQuote = false
        };

        await _chatRepository.AddMessageAsync(chatMessage);
        await Clients.All.SendAsync("ReceiveMessage", user.UserName, message, chatMessage.Timestamp);
    }

    public async Task JoinChat()
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "ChatRoom");
            await Clients.Group("ChatRoom").SendAsync("UserJoined", user.UserName);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = await GetCurrentUserAsync();
        if (user?.UserName != null)
        {
            await Clients.Group("ChatRoom").SendAsync("UserLeft", user.UserName);
        }
        await base.OnDisconnectedAsync(exception);
    }
    
    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return null;
            
        return await _userManager.GetUserAsync(Context.User);
    }

}