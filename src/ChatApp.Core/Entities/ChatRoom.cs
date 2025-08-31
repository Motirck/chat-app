namespace ChatApp.Core.Entities;

public class ChatRoom
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}