using Hangfire;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;

namespace Planora.Infrastructure.BackgroundJobs;

public sealed class ProcessRiskJob(
    IBackgroundJobClient backgroundJobClient,
    ILogger<ProcessRiskJob> logger,
    IAiAnalysisService aiAnalysis,
    IAnalysisJobRepository analysisJobRepository)
    : IProcessRiskJob
{
    public string Enqueue(ProccessRiskJobAiRequest request)
    {
        var jobId =
            backgroundJobClient.Enqueue<ProcessRiskJob>(
                x => x.Execute(request));

        logger.LogInformation(
            "Risk job enqueued for ParcelId {ParcelId} with HangfireJobId {JobId}",
            request.ParcelId, jobId);

        return jobId;
    }

    public async Task<Result<Success>> Execute(ProccessRiskJobAiRequest request)
    {
        logger.LogInformation(
            "Risk job started for ParcelId {ParcelId}",
            request.ParcelId);

        string pythonJobId;

        try
        {
            pythonJobId = await aiAnalysis.ProccessRiskAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Risk job failed while calling AI service for ParcelId {ParcelId}",
                request.ParcelId);
            throw;
        }

        logger.LogInformation(
            "AI service accepted risk job for ParcelId {ParcelId}, PythonJobId {PythonJobId}",
            request.ParcelId, pythonJobId);

        var result = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: request.ParcelId,
            pythonJobId: pythonJobId,
            type: AnalysisType.Risk);

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
            "Risk job completed for ParcelId {ParcelId}, PythonJobId {PythonJobId}, AnalysisJobId {AnalysisJobId}",
            request.ParcelId, pythonJobId, result.Value.Id);

        return Result.Success;
    }
}
