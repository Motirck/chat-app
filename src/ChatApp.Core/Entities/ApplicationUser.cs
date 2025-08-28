using Microsoft.AspNetCore.Identity;

namespace ChatApp.Core.Entities;

/// <summary>
/// Extended Identity user with additional properties for the chat application
/// </summary>
public class ApplicationUser : IdentityUser

{
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool IsOnline { get; set; }
    
    // Navigation properties
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

}