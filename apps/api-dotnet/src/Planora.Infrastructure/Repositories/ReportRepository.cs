using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Enums;
using Planora.Domain.Reports;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Repositories;

public sealed class ReportRepository(PlanoraDbContext context) : IReportRepository
{
    public async Task<Report?> GetLatestCompletedReportByParcelIdAsync(Guid parcelId, CancellationToken cancellationToken = default)
    {
        return await context.Reports
            .Include(r => r.Modules)
            .Include(r => r.Files)
            .Where(r => r.ParcelId == parcelId && r.Status == ReportStatus.Completed)
            .OrderByDescending(r => r.ProcessingCompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Report?> GetInProgressReportByParcelIdAsync(Guid parcelId, CancellationToken cancellationToken = default)
    {
        return await context.Reports
            .Include(r => r.Modules)
            .Where(r => r.ParcelId == parcelId && r.Status != ReportStatus.Completed && r.Status != ReportStatus.PendingPayment)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
