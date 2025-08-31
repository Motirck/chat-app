using ChatApp.Core.Entities;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Tests.Infrastructure;

public class ChatRepositoryTests
{
    private static ChatDbContext CreateDb()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new ChatDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact(DisplayName = "ChatRepository: AddMessageAsync persists"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public async Task Add_Persists()
    {
        await using var db = CreateDb();
        var repo = new ChatRepository(db);
        // Seed required user for FK
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "john", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var msg = new ChatMessage { Content = "hello", Timestamp = DateTime.UtcNow, UserId = "u1", Username = "john" };

        var saved = await repo.AddMessageAsync(msg);

        saved.Id.Should().NotBe(0);
        (await db.ChatMessages.CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "ChatRepository: GetLastMessagesAsync returns last N in ascending order"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public async Task GetLast_Returns_In_Ascending()
    {
        await using var db = CreateDb();
        var repo = new ChatRepository(db);
        var now = DateTime.UtcNow;
        // Seed FK user
        db.Users.Add(new ApplicationUser { Id = "u", UserName = "u", CreatedAt = now, LastLoginAt = now });
        await db.SaveChangesAsync();

        // Insert 5 messages via repository to ensure SaveChanges is called
        for (int i = 0; i < 5; i++)
        {
            await repo.AddMessageAsync(new ChatMessage { Content = $"m{i}", Timestamp = now.AddMinutes(i), UserId = "u", Username = "u" });
        }

        // Sanity check data exists in set
        (await db.ChatMessages.CountAsync()).Should().Be(5);

        var last3 = (await repo.GetLastMessagesAsync(3)).ToList();

        last3.Should().HaveCount(3);
        last3.Select(m => m.Content).Should().ContainInOrder("m2","m3","m4");
        last3.Should().BeInAscendingOrder(m => m.Timestamp);
    }
}
