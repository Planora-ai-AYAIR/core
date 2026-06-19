using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Application.Features.Notifications.Dtos;
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
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
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
            expansiveSoil?.Level ?? DeriveRiskLevel(request.Payload.ExpansiveSoilRisk),
            expansiveSoil?.Factors is not null ? JsonSerializer.Serialize(expansiveSoil.Factors) : null,
            expansiveSoil?.ReplacementDepth,
            liquefaction?.Level ?? DeriveRiskLevel(request.Payload.LiquefactionRisk),
            liquefaction?.Factors is not null ? JsonSerializer.Serialize(liquefaction.Factors) : null,
            liquefaction?.Susceptibility);

        await riskResultRepository.AddAsync(riskResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await PublishCompletionNotificationAsync(analysisJob, ct);

        logger.LogInformation("Successfully processed risk completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }

    private async Task PublishCompletionNotificationAsync(AnalysisJob job, CancellationToken ct)
    {
        var parcel = await parcelRepository.GetByIdAsync(job.ParcelId, ct);
        if (parcel is null) return;

        var data = JsonSerializer.Serialize(new
        {
            parcelId = parcel.Id,
            moduleType = job.Type.ToString(),
            analysisJobId = job.Id,
            link = $"/parcels/{parcel.Id}/reports/{job.Type.ToString().ToLower()}"
        });

        var result = Notification.Create(
            id: Guid.NewGuid(),
            userId: parcel.UserId,
            type: NotificationType.ModuleCompleted,
            title: $"{job.Type} analysis complete",
            message: $"{job.Type} analysis complete for Parcel #{parcel.Id.ToString()[..8]}",
            data: data);

        if (result.IsError) return;

        await notificationRepository.AddAsync(result.Value, ct);
        var dto = new NotificationDto(
            result.Value.Id, result.Value.Type,
            result.Value.Title, result.Value.Message,
            Link: ExtractLink(data), Data: data,
            result.Value.CreatedAt, result.Value.IsRead);
        await notificationPublisher.PublishAsync(parcel.UserId, dto, ct);
    }

    private static string? ExtractLink(string? data) =>
        data is null ? null : JsonDocument.Parse(data).RootElement
            .TryGetProperty("link", out var l) ? l.GetString() : null;
}
