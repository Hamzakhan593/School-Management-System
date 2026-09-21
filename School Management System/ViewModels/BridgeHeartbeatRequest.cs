using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class BridgeHeartbeatRequest
{
    [Required, StringLength(80)]
    public string DeviceCode { get; set; } = string.Empty;

    [StringLength(300)]
    public string? StatusMessage { get; set; }
}
