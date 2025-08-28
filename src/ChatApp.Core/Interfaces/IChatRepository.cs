using ChatApp.Core.Entities;

namespace ChatApp.Core.Interfaces;

public interface IChatRepository
{
    Task<IEnumerable<ChatMessage>> GetLastMessagesAsync(int count = 50);
    Task<ChatMessage> AddMessageAsync(ChatMessage message);
    Task<ApplicationUser?> GetUserByUsernameAsync(string username);
    Task<ApplicationUser?> GetUserByIdAsync(string id);
}