using Hangfire;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
using Planora.Application.Common.Helpers;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;

namespace Planora.Infrastructure.BackgroundJobs;

public sealed class ProcessSoilJob(
    IBackgroundJobClient backgroundJobClient,
    ILogger<ProcessSoilJob> logger,
    IAiAnalysisService aiAnalysis,
    IAnalysisJobRepository analysisJobRepository,
    IParcelRepository parcelRepository)
    : IProcessSoilJob
{
    public string Enqueue(Guid parcelId, Guid analysisJobId)
    {
        var jobId = backgroundJobClient.Enqueue<ProcessSoilJob>(
            x => x.Execute(parcelId, analysisJobId));

        logger.LogInformation(
            "Soil job enqueued for ParcelId {ParcelId}, AnalysisJobId {AnalysisJobId}, HangfireJobId {HangfireJobId}",
            parcelId, analysisJobId, jobId);

        return jobId;
    }

    public async Task<Result<Success>> Execute(Guid parcelId, Guid analysisJobId)
    {
        logger.LogInformation(
            "Soil job started for ParcelId {ParcelId}, AnalysisJobId {AnalysisJobId}",
            parcelId, analysisJobId);

        var parcel = await parcelRepository.GetByIdAsync(parcelId, CancellationToken.None);
        if (parcel is null)
        {
            logger.LogError("Parcel {ParcelId} not found for soil job", parcelId);
            return AnalysisJobErrors.NotFound;
        }

        var analysisJob = await analysisJobRepository.GetByIdAsync(analysisJobId, CancellationToken.None);
        if (analysisJob is null)
        {
            logger.LogError("AnalysisJob {AnalysisJobId} not found", analysisJobId);
            return AnalysisJobErrors.NotFound;
        }

        var request = new ProccessSoilJobAiRequest(
            JobId: analysisJob.Id.ToString(),
            ParcelId: parcel.Id.ToString(),
            GeoJson: parcel.Boundary.ToAiGeoJsonPolygon(),
            Bbox: parcel.Boundary.ToAiBoundingBox());

        string pythonJobId;
        try
        {
            pythonJobId = await aiAnalysis.ProccessSoilAsync(request, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Soil job failed while calling AI service for ParcelId {ParcelId}",
                parcelId);
            throw;
        }

        var setJobIdResult = analysisJob.SetPythonJobId(pythonJobId);
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
            "Soil job accepted by AI for ParcelId {ParcelId}, AnalysisJobId {AnalysisJobId}, PythonJobId {PythonJobId}",
            parcelId, analysisJobId, pythonJobId);

        return Result.Success;
    }
}
