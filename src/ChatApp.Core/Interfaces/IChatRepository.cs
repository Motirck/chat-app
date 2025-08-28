using ChatApp.Core.Entities;

namespace ChatApp.Core.Interfaces;

public interface IChatRepository
{
    public interface IChatRepository
    {
        Task<IEnumerable<ChatMessage>> GetLastMessagesAsync(int count = 50);
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByIdAsync(int id);
        Task<User> CreateUserAsync(User user);
        Task<bool> ValidateUserCredentialsAsync(string username, string passwordHash);
    }

}