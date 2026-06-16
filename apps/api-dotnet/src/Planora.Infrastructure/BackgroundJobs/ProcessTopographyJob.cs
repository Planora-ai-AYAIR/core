using Hangfire;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;

namespace Planora.Infrastructure.BackgroundJobs;
public sealed class ProcessTopographyJob(
    IBackgroundJobClient backgroundJobClient,
    ILogger<ProcessTopographyJob> logger,
    IAiAnalysisService aiAnalysis,
    IAnalysisJobRepository analysisJobRepository)
: IProcessTopographyJob
{
    public string Enqueue(ProccessTopographyJobAiRequest request)
    {
        var jobId =
            backgroundJobClient.Enqueue<ProcessTopographyJob>(
                x => x.Execute(request));
        
        logger.LogInformation(
            "Topography job enqueued for ParcelId {ParcelId} with HangfireJobId {JobId}",
            request.ParcelId, jobId);
        
        return jobId;
    }

    public async Task<Result<Success>> Execute(ProccessTopographyJobAiRequest request)
    {
        logger.LogInformation(
            "Topography job started for ParcelId {ParcelId}",
            request.ParcelId);

        string pythonJobId;
        
        try
        {
            pythonJobId = await aiAnalysis.ProccessTopographyAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Topography job failed while calling AI service for ParcelId {ParcelId}",
                request.ParcelId);
            throw;
        }

        logger.LogInformation(
            "AI service accepted topography job for ParcelId {ParcelId}, PythonJobId {PythonJobId}",
            request.ParcelId, pythonJobId);

        var result = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: request.ParcelId,
            pythonJobId: pythonJobId,
            type: AnalysisType.Topography
        );

        if (result.IsError)
        {
            logger.LogError(
                "Failed to create AnalysisJob entity for ParcelId {ParcelId}, PythonJobId {PythonJobId}. Error: {Error}",
                request.ParcelId, pythonJobId, result.TopError.Description);
            
            return result.TopError;
        }

        await analysisJobRepository.AddAsync(result.Value);

        logger.LogInformation(
            "Topography job completed for ParcelId {ParcelId}, PythonJobId {PythonJobId}, AnalysisJobId {AnalysisJobId}",
            request.ParcelId, pythonJobId, result.Value.Id);

        return Result.Success;
    }
}
