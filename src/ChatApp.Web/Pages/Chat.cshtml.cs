using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Pages;

/// <summary>
/// Chat room page that displays real-time messaging interface with multi-room support
/// </summary>
[Authorize]
public class ChatModel : PageModel
{
    private readonly IChatRepository _chatRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ChatModel> _logger;

    public ChatModel(
        IChatRepository chatRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<ChatModel> logger)
    {
        _chatRepository = chatRepository;
        _userManager = userManager;
        _logger = logger;
    }

    public IEnumerable<ChatMessage> RecentMessages { get; set; } = new List<ChatMessage>();
    public IEnumerable<ChatRoom> AvailableRooms { get; set; } = new List<ChatRoom>();
    public string CurrentRoomId { get; set; } = "lobby";
    public ApplicationUser? CurrentUser { get; set; }

    /// <summary>
    /// Loads the chat page with recent messages and user information
    /// </summary>
    public async Task OnGetAsync([FromQuery] string? room)
    {
        try
        {
            // Determine current room
            CurrentRoomId = string.IsNullOrWhiteSpace(room) ? "lobby" : room;

            // Get current user
            CurrentUser = await _userManager.GetUserAsync(User);
            
            if (CurrentUser != null)
            {
                // Update user online status
                CurrentUser.IsOnline = true;
                CurrentUser.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(CurrentUser);

                _logger.LogInformation("User {Username} accessed chat room {RoomId}", CurrentUser.UserName, CurrentRoomId);
            }

            // Load rooms and recent messages for the selected room
            AvailableRooms = await _chatRepository.GetAvailableRoomsAsync();
            RecentMessages = await _chatRepository.GetLastMessagesAsync(50, CurrentRoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chat page for user {UserId}", User.Identity?.Name);
            RecentMessages = new List<ChatMessage>();
            AvailableRooms = new List<ChatRoom>();
        }
    }
}