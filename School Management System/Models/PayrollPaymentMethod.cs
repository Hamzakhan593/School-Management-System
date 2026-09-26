using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Models;

public enum PayrollPaymentMethod
{
    Cash = 1,
    [Display(Name = "Bank Transfer")]
    BankTransfer = 2,
    Cheque = 3,
    [Display(Name = "Digital Transfer")]
    DigitalTransfer = 4,
    Other = 5
}
