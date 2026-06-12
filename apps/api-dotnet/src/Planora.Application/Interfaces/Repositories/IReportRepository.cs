using Planora.Domain.Reports;

namespace Planora.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<Report?> GetByParcelIdWithDetailsAsync(Guid parcelId, CancellationToken cancellationToken = default);
}
