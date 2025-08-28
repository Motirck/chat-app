using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{ 
    private readonly ChatDbContext _dbContext;
    
    public ChatRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IEnumerable<ChatMessage>> GetLastMessagesAsync(int count = 50)
    {
        return await _dbContext.ChatMessages
            .Include(m => m.User)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }
    
    public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
    {
        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync();
        return message;
    }
}