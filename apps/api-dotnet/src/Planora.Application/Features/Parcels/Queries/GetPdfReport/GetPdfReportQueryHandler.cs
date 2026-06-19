using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Dtos.PdfReport;
using Planora.Application.Features.Parcels.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetPdfReport;

public sealed class GetPdfReportQueryHandler(
    IReportRepository reportRepository,
    IStorageService storageService,
    IHybridCacheService cacheService,
    ILogger<GetPdfReportQueryHandler> logger)
    : IRequestHandler<GetPdfReportQuery, Result<PdfReportResponse>>
{
    private static readonly TimeSpan UrlExpiry = TimeSpan.FromHours(1);
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(50);

    public async Task<Result<PdfReportResponse>> Handle(GetPdfReportQuery request, CancellationToken ct)
    {
        var cacheKey = $"pdf-report:{request.ParcelId}";

        var cached = await cacheService.GetAsync<PdfReportResponse>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        var report = await reportRepository.GetLatestCompletedReportByParcelIdAsync(request.ParcelId, ct);

        if (report is null)
        {
            return ReportErrors.NotFound;
        }

        if (report.Status != ReportStatus.Completed)
        {
            return ReportErrors.NotReady;
        }

        var pdfModule = report.Modules.FirstOrDefault(m => m.ModuleType == ModuleType.PdfReport);

        if (pdfModule is null || string.IsNullOrWhiteSpace(pdfModule.OutputS3Key))
        {
            return ReportErrors.PdfModuleMissing;
        }

        var downloadUrl = await storageService.GetPreSignedUrlAsync(pdfModule.OutputS3Key, UrlExpiry);

        var response = new PdfReportResponse(
            request.ParcelId,
            downloadUrl,
            pdfModule.PageCount,
            pdfModule.FileSizeBytes,
            pdfModule.CompletedAt ?? pdfModule.UpdatedAt
        );

        await cacheService.SetAsync(cacheKey, response, new CacheEntryOptions
        {
            Expiration = CacheExpiry,
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            Tags = ["pdf-report", $"parcel:{request.ParcelId}"]
        }, ct);

        return response;
    }
}
