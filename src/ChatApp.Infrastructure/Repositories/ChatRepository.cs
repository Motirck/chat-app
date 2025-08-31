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
    
    public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
    {
        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync();
        return message;
    }
    
    public async Task<IEnumerable<ChatMessage>> GetLastMessagesAsync(int count, string? roomId = null)
    {
        var query = _dbContext.ChatMessages
            .Include(m => m.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(roomId))
        {
            query = query.Where(m => m.RoomId == roomId);
        }

        return await query
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<ChatRoom>> GetAvailableRoomsAsync()
    {
        return await _dbContext.ChatRooms
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }
}