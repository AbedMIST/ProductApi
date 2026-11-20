namespace ProductApi.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string PasswordHash { get; set; } = string.Empty; // never store the plain password
    public DateTime CreatedAt { get; set; }
}
