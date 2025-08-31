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

    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<ChatRoom> ChatRooms { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.RoomId).IsRequired().HasDefaultValue("lobby");
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
        });

        // ChatRoom configuration
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Id).ValueGeneratedNever();
            
            entity.HasMany(e => e.Messages)
                .WithOne()
                .HasForeignKey(e => e.RoomId);
        });

        // Seed default rooms with static DateTime values
        modelBuilder.Entity<ChatRoom>().HasData(
            new ChatRoom { Id = "lobby", Name = "Lobby", Description = "General discussion", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ChatRoom { Id = "general", Name = "General", Description = "General chat", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ChatRoom { Id = "tech", Name = "Tech Talk", Description = "Technology discussions", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
