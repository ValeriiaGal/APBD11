using System.ComponentModel.DataAnnotations;

namespace DTOs;

public class RegisterDto
{
    [Required]
    [RegularExpression("^[^\\d].*")]
    public string Username { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 12)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*\\W).+$")]
    public string Password { get; set; } = null!;

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public string Role { get; set; } = null!; // "Admin" or "User"
}