using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class BiometricDeviceFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "Device Code")]
    public string DeviceCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Model { get; set; }

    [StringLength(80)]
    [Display(Name = "Connection Type")]
    public string? ConnectionType { get; set; }

    [StringLength(80)]
    [Display(Name = "IP Address")]
    public string? IpAddress { get; set; }

    [StringLength(150)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;
}
