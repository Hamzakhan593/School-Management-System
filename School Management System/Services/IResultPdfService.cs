using School_Management_System.ViewModels;

namespace School_Management_System.Services;

public interface IResultPdfService
{
    Task<byte[]> CreateResultCardPdfAsync(ResultCardViewModel model, CancellationToken cancellationToken = default);
    Task<byte[]> CreateBulkResultCardsPdfAsync(IReadOnlyList<ResultCardViewModel> models, CancellationToken cancellationToken = default);
    Task<byte[]> CreateClassResultSheetPdfAsync(ClassResultViewModel model, CancellationToken cancellationToken = default);
}
