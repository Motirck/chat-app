namespace ChatApp.Core.Dtos;

public record StockQuoteDto
{
    public string StockCode { get; init; } = string.Empty;
    public string Quote { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string RoomId { get; init; } = "lobby";
    public DateTime Timestamp { get; init; }
}
