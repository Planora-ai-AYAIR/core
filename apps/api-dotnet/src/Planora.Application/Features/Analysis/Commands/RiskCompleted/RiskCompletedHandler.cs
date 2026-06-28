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

        // Use nested sub-results when present, otherwise fall back to flat scores with derived levels
        var overallRiskLevel = request.Payload.OverallRiskLevel ?? DeriveRiskLevel(request.Payload.OverallRiskScore);

        var flood = request.Payload.Flood;
        var seismic = request.Payload.Seismic;
        var expansiveSoil = request.Payload.ExpansiveSoil;
        var liquefaction = request.Payload.Liquefaction;

        var mitigationSuggestionsJson = request.Payload.MitigationSuggestions is not null
            ? JsonSerializer.Serialize(request.Payload.MitigationSuggestions)
            : null;

        var riskResult = new RiskResult(
            analysisJob.Id,
            flood?.Score ?? request.Payload.FloodRiskScore,
            seismic?.Score ?? request.Payload.SeismicRiskScore,
            expansiveSoil?.Score ?? request.Payload.ExpansiveSoilRisk,
            liquefaction?.Score ?? request.Payload.LiquefactionRisk,
            request.Payload.OverallRiskScore,
            overallRiskLevel,
            flood?.Level ?? DeriveRiskLevel(request.Payload.FloodRiskScore),
            flood?.Factors is not null ? JsonSerializer.Serialize(flood.Factors) : null,
            flood?.GeoJsonUrl,
            seismic?.Level ?? DeriveRiskLevel(request.Payload.SeismicRiskScore),
            seismic?.Factors is not null ? JsonSerializer.Serialize(seismic.Factors) : null,
            seismic?.Source,
            seismicZone: seismic?.Zone,
            expansiveSoil?.Level ?? DeriveRiskLevel(request.Payload.ExpansiveSoilRisk),
            expansiveSoil?.Factors is not null ? JsonSerializer.Serialize(expansiveSoil.Factors) : null,
            expansiveSoil?.ReplacementDepth,
            liquefaction?.Level ?? DeriveRiskLevel(request.Payload.LiquefactionRisk),
            liquefaction?.Factors is not null ? JsonSerializer.Serialize(liquefaction.Factors) : null,
            liquefaction?.Susceptibility,
            liquefactionMethodology: liquefaction?.Methodology,
            riskHeatmapTileUrl: request.Payload.RiskHeatmapTileUrl,
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
