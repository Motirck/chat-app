namespace ChatApp.Core.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsStockQuote { get; set; } = false;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}