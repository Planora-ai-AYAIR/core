using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Logging;
using NetTopologySuite.IO;
using Planora.Application.Common.Dtos;
using Planora.Application.Common.Helpers;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;

namespace Planora.Infrastructure.BackgroundJobs;

public sealed class ProcessAggregatedAnalysisJob(
    IBackgroundJobClient backgroundJobClient,
    ILogger<ProcessAggregatedAnalysisJob> logger,
    IAiAnalysisService aiAnalysis,
    IAnalysisJobRepository analysisJobRepository,
    IParcelRepository parcelRepository) : IProcessAggregatedAnalysisJob
{
    public string Enqueue(Guid parcelId, Guid analysisJobId)
    {
        var jobId = backgroundJobClient.Enqueue<ProcessAggregatedAnalysisJob>(
            x => x.Execute(parcelId, analysisJobId));

        logger.LogInformation(
            "Aggregated analysis job enqueued for ParcelId {ParcelId}, AnalysisJobId {AnalysisJobId}, HangfireJobId {HangfireJobId}",
            parcelId, analysisJobId, jobId);

        return jobId;
    }

    public async Task<Result<Success>> Execute(Guid parcelId, Guid analysisJobId)
    {
        logger.LogInformation(
            "Aggregated analysis job started for ParcelId {ParcelId}, AnalysisJobId {AnalysisJobId}",
            parcelId, analysisJobId);

        var parcel = await parcelRepository.GetByIdAsync(parcelId, CancellationToken.None);
        if (parcel is null)
        {
            logger.LogError("Parcel {ParcelId} not found for aggregated analysis job", parcelId);
            return AnalysisJobErrors.NotFound;
        }

        var analysisJob = await analysisJobRepository.GetByIdAsync(analysisJobId, CancellationToken.None);
        if (analysisJob is null)
        {
            logger.LogError("AnalysisJob {AnalysisJobId} not found", analysisJobId);
            return AnalysisJobErrors.NotFound;
        }

        var geoJson = parcel.Boundary.ToGeoJson();
        var envelope = parcel.Boundary.EnvelopeInternal;
        var coordinates = ParseCoordinatesFromGeoJson(geoJson);

        var request = new SubmitAiAnalysisJobRequest(
            ParcelId: parcel.Id.ToString(),
            Parcel: new ParcelInfo(parcel.Name, parcel.AreaHectares * 10_000m),
            BoundingBox: new BoundingBoxInfo(
                envelope.MinY,
                envelope.MinX,
                envelope.MaxY,
                envelope.MaxX),
            Geometry: new GeometryInfo("Polygon", coordinates),
            AnalysisOptions: new AnalysisOptionsInfo(
                analysisJob.Options?.IncludeTopography ?? false,
                analysisJob.Options?.IncludeSoil ?? false,
                analysisJob.Options?.IncludeBearing ?? false,
                analysisJob.Options?.IncludeRisk ?? false,
                analysisJob.Options?.IncludeBorehole ?? false));

        AiAnalysisJobResponse response;
        try
        {
            response = await aiAnalysis.SubmitAnalysisJobAsync(request, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Aggregated analysis job failed while calling AI service for ParcelId {ParcelId}",
                parcelId);
            throw;
        }

        var setJobIdResult = analysisJob.SetPythonJobId(response.Data.JobId);
        if (setJobIdResult.IsError)
        {
            logger.LogError(
                "Failed to set PythonJobId for AnalysisJob {AnalysisJobId}: {Error}",
                analysisJobId, setJobIdResult.TopError.Description);
            return setJobIdResult.TopError;
        }

        var markRunningResult = analysisJob.MarkAsRunning();
        if (markRunningResult.IsError)
        {
            logger.LogError(
                "Failed to mark AnalysisJob {AnalysisJobId} as Running: {Error}",
                analysisJobId, markRunningResult.TopError.Description);
            return markRunningResult.TopError;
        }

        await analysisJobRepository.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation(
            "Aggregated analysis job accepted by AI service for ParcelId {ParcelId}, PythonJobId {PythonJobId}",
            parcelId, response.Data.JobId);

        return Result.Success;
    }

    private static List<List<List<double>>> ParseCoordinatesFromGeoJson(string geoJson)
    {
        using var doc = JsonDocument.Parse(geoJson);
        var coords = doc.RootElement.GetProperty("coordinates");
        return JsonSerializer.Deserialize<List<List<List<double>>>>(coords.GetRawText())!;
    }
}
