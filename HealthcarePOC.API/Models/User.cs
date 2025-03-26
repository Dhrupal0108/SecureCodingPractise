namespace HealthcarePOC.API.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // hashed password
    public string Role { get; set; } = string.Empty; // User Role
}
