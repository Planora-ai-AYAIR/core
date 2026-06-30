using Microsoft.AspNetCore.SignalR;
using Planora.Application.Interfaces.Services;

namespace Planora.Api.Hubs;

public sealed class ReportNotifier(
    IHubContext<NotificationHub> hubContext,
    ILogger<ReportNotifier> logger)
    : IReportNotifier
{
    public async Task NotifyReportGeneratedAsync(
        Guid parcelId, Guid reportId, CancellationToken ct)
    {
        var groupName = $"parcel:{parcelId}";
        await hubContext.Clients.Group(groupName)
            .SendAsync("ReportGenerated", new
            {
                Event = "ReportGenerated",
                ParcelId = parcelId,
                ReportJobId = reportId,
                GeneratedAt = DateTime.UtcNow,
                DownloadUrl = $"/api/reports/{reportId}",
                Message = "Your PDF report is ready for download."
            }, ct);
    }

    public async Task NotifyReportFailedAsync(
        Guid parcelId, Guid reportId, string errorMessage, CancellationToken ct)
    {
        var groupName = $"parcel:{parcelId}";
        await hubContext.Clients.Group(groupName)
            .SendAsync("ReportFailed", new
            {
                Event = "ReportFailed",
                ParcelId = parcelId,
                ReportJobId = reportId,
                FailedAt = DateTime.UtcNow,
                ErrorCode = "PDF_RENDER_ERROR",
                Message = errorMessage,
                Retryable = true
            }, ct);
    }
}