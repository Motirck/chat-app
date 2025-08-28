namespace ChatApp.Core.Dtos;

public record ChatMessageDto
{
    public int Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsStockQuote { get; init; }
}
