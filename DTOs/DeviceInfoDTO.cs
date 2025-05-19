namespace DTOs;

public class DeviceInfoDto
{
    public string DeviceType { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public object AdditionalProperties { get; set; } = null!;
    public EmployeeDto? CurrentEmployee { get; set; }
}
