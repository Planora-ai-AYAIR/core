using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Application.Features.Notifications.Dtos;
using Planora.Application.Features.Parcels.Dtos.Webhook;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Notifications;
using Planora.Domain.Shared.Results;
using System.Text.Json;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Features.Analysis.Commands.RiskCompleted;

public sealed class RiskCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    IRiskResultRepository riskResultRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<RiskCompletedHandler> logger) : IRequestHandler<RiskCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    private static string? DeriveRiskLevel(int score) => score switch
    {
        <= 20 => "Very Low",
        <= 40 => "Low",
        <= 60 => "Moderate",
        <= 80 => "High",
        _ => "Very High"
    };

    public async Task<Result<AnalysisJobProcessedResponse>> Handle(RiskCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing risk completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

        var analysisJob = await analysisJobRepository.GetByPythonJobIdAsync(request.PythonJobId, ct);

        if (analysisJob is null)
        {
            logger.LogWarning("AnalysisJob not found for PythonJobId: {PythonJobId}", request.PythonJobId);
            return AnalysisJobErrors.NotFound;
        }

        var markAsCompletedResult = analysisJob.MarkAsCompleted();
        if (markAsCompletedResult.IsError)
        {
            logger.LogError("Failed to update status for AnalysisJob {AnalysisJobId}", analysisJob.Id);
            return AnalysisJobErrors.FaildStatusUpdate;
        }

        var overallRiskLevel = request.Payload.OverallRiskLevel ?? DeriveRiskLevel(request.Payload.OverallScore);

        var breakdown = request.Payload.RiskBreakdown;
        var assets = request.Payload.VisualizationAssets;
        var flood = breakdown?.Flood;
        var seismic = breakdown?.Seismic;
        var expansiveSoil = breakdown?.ExpansiveSoil;
        var liquefaction = breakdown?.Liquefaction;

        var mitigationSuggestionsJson = request.Payload.MitigationSuggestions is not null
            ? JsonSerializer.Serialize(request.Payload.MitigationSuggestions)
            : null;

        var riskResult = new RiskResult(
            analysisJob.Id,
            flood?.Score ?? 0,
            seismic?.Score ?? 0,
            expansiveSoil?.Score ?? 0,
            liquefaction?.Score ?? 0,
            request.Payload.OverallScore,
            overallRiskLevel,
            flood?.Level ?? DeriveRiskLevel(flood?.Score ?? 0),
            flood?.Factors is not null ? JsonSerializer.Serialize(flood.Factors) : null,
            flood?.ZonesGeoJsonUrl,
            seismic?.Level ?? DeriveRiskLevel(seismic?.Score ?? 0),
            seismic?.Factors is not null ? JsonSerializer.Serialize(seismic.Factors) : null,
            seismic?.Source,
            seismicZone: seismic?.Zone,
            expansiveSoil?.Level ?? DeriveRiskLevel(expansiveSoil?.Score ?? 0),
            expansiveSoil?.Factors is not null ? JsonSerializer.Serialize(expansiveSoil.Factors) : null,
            expansiveSoil?.ReplacementDepthMeters,
            liquefaction?.Level ?? DeriveRiskLevel(liquefaction?.Score ?? 0),
            liquefaction?.Factors is not null ? JsonSerializer.Serialize(liquefaction.Factors) : null,
            liquefaction?.Susceptibility,
            liquefactionMethodology: liquefaction?.Methodology,
            riskHeatmapTileUrl: assets?.RiskHeatmapTileUrl,
            mitigationSuggestionsJson: mitigationSuggestionsJson);

        await riskResultRepository.AddAsync(riskResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.RiskCompleted, request.Payload, notificationPublisher, ct);

        logger.LogInformation("Successfully processed risk completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
