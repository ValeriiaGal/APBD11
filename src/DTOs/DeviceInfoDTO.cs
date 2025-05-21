namespace DTOs;

public class DeviceInfoDto
{
    public string Name { get; set; } = null!;
    public string DeviceTypeName { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> AdditionalProperties { get; set; } = null!;
    public EmployeeDto? CurrentEmployee { get; set; }
}

