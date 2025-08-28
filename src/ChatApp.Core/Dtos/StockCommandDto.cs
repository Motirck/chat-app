namespace ChatApp.Core.Dtos;

public record StockCommandDto
{
    public string StockCode { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
