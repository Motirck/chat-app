using ChatApp.Core.Entities;

namespace ChatApp.Core.Interfaces;

public interface IChatRepository
{
    Task<ChatMessage> AddMessageAsync(ChatMessage message);
    Task<IEnumerable<ChatMessage>> GetLastMessagesAsync(int count, string? roomId = null);
    Task<IEnumerable<ChatRoom>> GetAvailableRoomsAsync();
}