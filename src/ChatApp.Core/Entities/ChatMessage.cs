namespace ChatApp.Core.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public required string UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsStockQuote { get; set; }
    public string RoomId { get; set; } = "lobby"; // Default room
    
    public virtual ApplicationUser User { get; set; } = null!;
}