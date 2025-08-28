namespace ChatApp.Core.Dtos;

public record UserDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
}
