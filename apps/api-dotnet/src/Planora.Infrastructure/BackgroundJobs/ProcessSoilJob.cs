using Hangfire;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
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
    IAnalysisJobRepository analysisJobRepository)
    : IProcessSoilJob
{
    public string Enqueue(ProccessSoilJobAiRequest request)
    {
        var jobId =
            backgroundJobClient.Enqueue<ProcessSoilJob>(
                x => x.Execute(request));

        logger.LogInformation(
            "Soil job enqueued for ParcelId {ParcelId} with HangfireJobId {JobId}",
            request.ParcelId, jobId);

        return jobId;
    }

    public async Task<Result<Success>> Execute(ProccessSoilJobAiRequest request)
    {
        logger.LogInformation(
            "Soil job started for ParcelId {ParcelId}",
            request.ParcelId);

        string pythonJobId;

        try
        {
            pythonJobId = await aiAnalysis.ProccessSoilAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Soil job failed while calling AI service for ParcelId {ParcelId}",
                request.ParcelId);
            throw;
        }

        logger.LogInformation(
            "AI service accepted soil job for ParcelId {ParcelId}, PythonJobId {PythonJobId}",
            request.ParcelId, pythonJobId);

        var result = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: request.ParcelId,
            pythonJobId: pythonJobId,
            type: AnalysisType.Soil);

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
            "Soil job completed for ParcelId {ParcelId}, PythonJobId {PythonJobId}, AnalysisJobId {AnalysisJobId}",
            request.ParcelId, pythonJobId, result.Value.Id);

        return Result.Success;
    }
}
