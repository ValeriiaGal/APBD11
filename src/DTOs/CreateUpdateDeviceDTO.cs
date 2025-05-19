using System.ComponentModel.DataAnnotations;

namespace DTOs;

public class CreateUpdateDeviceDto
{
    [Required]
    public string DeviceType { get; set; } = null!;

    public bool IsEnabled { get; set; }

    [Required]
    public object AdditionalProperties { get; set; } = null!;
}
