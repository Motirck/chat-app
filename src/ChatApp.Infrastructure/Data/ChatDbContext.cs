using ChatApp.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Data;

/// <summary>
/// Database context that extends IdentityDbContext for user authentication
/// and includes chat-specific entities
/// </summary>
public class ChatDbContext : IdentityDbContext<ApplicationUser>
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }
    
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Important: Call base first for Identity tables
        
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastLoginAt).IsRequired();
        });
        
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.Messages)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}