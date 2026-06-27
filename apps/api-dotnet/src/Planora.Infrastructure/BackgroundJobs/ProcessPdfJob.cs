using Hangfire;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;

namespace Planora.Infrastructure.BackgroundJobs;

public sealed class ProcessPdfJob(
    IBackgroundJobClient backgroundJobClient,
    ILogger<ProcessPdfJob> logger,
    IAiAnalysisService aiAnalysis,
    IAnalysisJobRepository analysisJobRepository)
    : IProcessPdfJob
{
    public string Enqueue(ProccessPdfJobAiRequest request)
    {
        var jobId =
            backgroundJobClient.Enqueue<ProcessPdfJob>(
                x => x.Execute(request));

        logger.LogInformation(
            "PDF job enqueued with HangfireJobId {JobId}", jobId);

        return jobId;
    }

    public async Task<Result<Success>> Execute(ProccessPdfJobAiRequest request)
    {
        logger.LogInformation(
            "PDF job started for JobId {JobId}, ParcelId {ParcelId}",
            request.JobId, request.ParcelId);

        string pythonJobId;

        try
        {
            pythonJobId = await aiAnalysis.ProccessPdfAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "PDF job failed while calling AI service for ParcelId {ParcelId}",
                request.ParcelId);
            throw;
        }

        logger.LogInformation(
            "AI service accepted PDF job for ParcelId {ParcelId}, PythonJobId {PythonJobId}",
            request.ParcelId, pythonJobId);

        if (!Guid.TryParse(request.ParcelId, out var parcelIdGuid))
        {
            logger.LogError("Invalid ParcelId format in PDF request: {ParcelId}", request.ParcelId);
            return AnalysisJobErrors.InvalidParcelId;
        }

        var result = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: parcelIdGuid,
            pythonJobId: pythonJobId,
            type: AnalysisType.Pdf);

        if (result.IsError)
        {
            logger.LogError(
                "Failed to create AnalysisJob entity for ParcelId {ParcelId}, PythonJobId {PythonJobId}. Error: {Error}",
                request.ParcelId, pythonJobId, result.TopError.Description);

            return result.TopError;
        }

        await analysisJobRepository.AddAsync(result.Value);
        await analysisJobRepository.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation(
            "PDF job completed for ParcelId {ParcelId}, PythonJobId {PythonJobId}, AnalysisJobId {AnalysisJobId}",
            request.ParcelId, pythonJobId, result.Value.Id);

        return Result.Success;
    }
}
