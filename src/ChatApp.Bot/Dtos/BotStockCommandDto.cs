namespace ChatApp.Bot.Dtos;

public record BotStockCommandDto
{
    public string StockCode { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string RoomId { get; init; } = "lobby";
    public DateTime Timestamp { get; init; }
}