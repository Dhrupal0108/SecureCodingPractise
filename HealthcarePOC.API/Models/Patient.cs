using System.Text.Json.Serialization;

namespace HealthcarePOC.API.Models;

public class Patient
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }

    [JsonIgnore]
    public string? PasswordHash { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MedicalRecord { get; set; }
}
