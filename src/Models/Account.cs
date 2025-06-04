using System.ComponentModel.DataAnnotations;

namespace API;

public class Account
{
    public int Id { get; set; }

    [Required]
    [RegularExpression(@"^[^\d]\w+$", ErrorMessage = "Username cannot start with a number.")]
    public string Username { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [Required]
    public int RoleId { get; set; }

    public int EmployeeId { get; set; }

    public Role Role { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
