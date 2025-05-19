namespace DTOs;

public class CreateUpdateDeviceDto
{
    public string DeviceType { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public object AdditionalProperties { get; set; } = null!;
}
