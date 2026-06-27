using Planora.Domain.Reports;

namespace Planora.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<Report?> GetLatestCompletedReportByParcelIdAsync(Guid parcelId, CancellationToken cancellationToken = default);
    Task<Report?> GetInProgressReportByParcelIdAsync(Guid parcelId, CancellationToken cancellationToken = default);
    Task AddAsync(Report report, CancellationToken ct);

    Task<Report?> GetByIdAsync(Guid reportId,CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

}
