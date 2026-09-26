using System.ComponentModel.DataAnnotations;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class LookupOption
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class FeesDashboardViewModel
{
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal CurrentMonthCharges { get; set; }
    public decimal CurrentMonthCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int OverdueStudentCount { get; set; }
    public int ChallansThisMonth { get; set; }
    public IReadOnlyList<FeePayment> RecentPayments { get; set; } = Array.Empty<FeePayment>();
    public IReadOnlyList<FeeChallan> RecentChallans { get; set; } = Array.Empty<FeeChallan>();
}

public class FeeHeadFormViewModel
{
    public int? Id { get; set; }
    [Required, StringLength(30)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    public FeeFrequency DefaultFrequency { get; set; } = FeeFrequency.Monthly;
    [Range(typeof(decimal), "0", "999999999")] public decimal DefaultAmount { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FeeHeadsViewModel
{
    public FeeHeadFormViewModel Form { get; set; } = new();
    public IReadOnlyList<FeeHead> Heads { get; set; } = Array.Empty<FeeHead>();
}

public class FeeStructureFormViewModel
{
    public int? Id { get; set; }
    [Required] public int AcademicSessionId { get; set; }
    [Required] public int FeeHeadId { get; set; }
    public FeeStructureScope Scope { get; set; } = FeeStructureScope.Class;
    public int? SchoolClassId { get; set; }
    public int? StudentId { get; set; }
    public int? TermId { get; set; }
    public FeeFrequency Frequency { get; set; } = FeeFrequency.Monthly;
    [Range(typeof(decimal), "0.01", "999999999")] public decimal Amount { get; set; }
    [DataType(DataType.Date)] public DateTime? EffectiveFrom { get; set; }
    [DataType(DataType.Date)] public DateTime? EffectiveTo { get; set; }
    [DataType(DataType.Date)] public DateTime? ChargeDate { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FeeStructuresViewModel
{
    public FeeStructureFormViewModel Form { get; set; } = new();
    public IReadOnlyList<FeeStructure> Structures { get; set; } = Array.Empty<FeeStructure>();
    public IReadOnlyList<LookupOption> Sessions { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<LookupOption> FeeHeads { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<LookupOption> Classes { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<LookupOption> Students { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<LookupOption> Terms { get; set; } = Array.Empty<LookupOption>();
}

public class StudentDiscountFormViewModel
{
    public int? Id { get; set; }
    [Required] public int StudentId { get; set; }
    public int? FeeHeadId { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    [Range(typeof(decimal), "0.01", "999999999")] public decimal Value { get; set; }
    [DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    [Required, StringLength(300)] public string ApprovalNote { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class StudentDiscountsViewModel
{
    public StudentDiscountFormViewModel Form { get; set; } = new();
    public IReadOnlyList<StudentDiscount> Discounts { get; set; } = Array.Empty<StudentDiscount>();
    public IReadOnlyList<LookupOption> Students { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<LookupOption> FeeHeads { get; set; } = Array.Empty<LookupOption>();
}

public class ChallanGenerationViewModel
{
    [Required] public int AcademicSessionId { get; set; }
    [DataType("month")] public DateTime BillingMonth { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public FeeBatchScope Scope { get; set; } = FeeBatchScope.WholeSchool;
    public int? StudentId { get; set; }
    public int? SchoolClassId { get; set; }
    public int? SectionId { get; set; }
    public List<int> SelectedStudentIds { get; set; } = new();
    public string PreviewToken { get; set; } = string.Empty;
    public bool IsPreview { get; set; }
    public FeeGenerationPreviewViewModel? Preview { get; set; }
    public IReadOnlyList<LookupOption> Sessions { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<LookupOption> Classes { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<LookupOption> Sections { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<LookupOption> Students { get; set; } = Array.Empty<LookupOption>();
}

public class FeeGenerationPreviewViewModel
{
    public int EligibleStudents { get; set; }
    public int WillGenerate { get; set; }
    public int WillSkipExisting { get; set; }
    public int WillSkipNoStructure { get; set; }
    public decimal EstimatedCurrentCharges { get; set; }
    public decimal ExistingPreviousOutstanding { get; set; }
    public List<FeeGenerationPreviewRowViewModel> Rows { get; set; } = new();
}

public class FeeGenerationPreviewRowViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string ClassSection { get; set; } = string.Empty;
    public decimal CurrentCharges { get; set; }
    public decimal PreviousOutstanding { get; set; }
    public string Result { get; set; } = string.Empty;
}

public class ChallansViewModel
{
    public int? BatchId { get; set; }
    public string? BillingPeriod { get; set; }
    public FeeChallanStatus? Status { get; set; }
    public int? SchoolClassId { get; set; }
    public string? Search { get; set; }
    public IReadOnlyList<FeeChallan> Challans { get; set; } = Array.Empty<FeeChallan>();
    public IReadOnlyList<LookupOption> Classes { get; set; } = Array.Empty<LookupOption>();
}

public class FeeChallanDetailsViewModel
{
    public FeeChallan Challan { get; set; } = null!;
    public decimal CurrentStudentOutstanding { get; set; }
    public decimal ActiveAllocatedAmount { get; set; }
}

public class PaymentEntryViewModel
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Required] public int StudentId { get; set; }
    public int? FromChallanId { get; set; }
    [Range(typeof(decimal), "0.01", "999999999")] public decimal Amount { get; set; }
    public FeePaymentMethod PaymentMethod { get; set; } = FeePaymentMethod.Cash;
    [StringLength(150)] public string? ReferenceNumber { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public decimal Outstanding { get; set; }
}

public class StudentLedgerViewModel
{
    public Student? Student { get; set; }
    public IReadOnlyList<LookupOption> StudentOptions { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<FeeChallan> Challans { get; set; } = Array.Empty<FeeChallan>();
    public IReadOnlyList<FeePayment> Payments { get; set; } = Array.Empty<FeePayment>();
    public decimal TotalCharges { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class DefaulterReportViewModel
{
    public DateTime AsOfDate { get; set; } = DateTime.Today;
    public int? SchoolClassId { get; set; }
    public decimal? MinimumAmount { get; set; }
    public int? MinimumOverduePeriods { get; set; }
    public IReadOnlyList<LookupOption> Classes { get; set; } = Array.Empty<LookupOption>();
    public IReadOnlyList<DefaulterRowViewModel> Rows { get; set; } = Array.Empty<DefaulterRowViewModel>();
}

public class DefaulterRowViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AdmissionNumber { get; set; } = string.Empty;
    public string ClassSection { get; set; } = string.Empty;
    public int OverduePeriods { get; set; }
    public DateTime OldestDueDate { get; set; }
    public decimal Outstanding { get; set; }
}

public class DailyCollectionViewModel
{
    public DateTime Date { get; set; } = DateTime.Today;
    public IReadOnlyList<FeePayment> Payments { get; set; } = Array.Empty<FeePayment>();
    public decimal Total { get; set; }
    public Dictionary<FeePaymentMethod, decimal> TotalsByMethod { get; set; } = new();
    public Dictionary<string, decimal> TotalsByReceiver { get; set; } = new();
    public Dictionary<string, decimal> TotalsByClass { get; set; } = new();
}
