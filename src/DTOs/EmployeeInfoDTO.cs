namespace DTOs;

public class EmployeeDetailsDto
{
    public string PassportNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public decimal Salary { get; set; }
    public PositionDto Position { get; set; } = null!;
    public DateTime HireDate { get; set; }
}