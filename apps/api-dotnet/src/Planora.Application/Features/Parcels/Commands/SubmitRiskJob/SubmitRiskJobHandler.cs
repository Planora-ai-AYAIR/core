using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Parcels.Dtos.SubmitRiskJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitRiskJob;

public sealed class SubmitRiskJobHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    IHybridCacheService cacheService,
    IProcessRiskJob processRiskJob,
    ILogger<SubmitRiskJobHandler> logger)
    : IRequestHandler<SubmitRiskJobCommand, Result<SubmitRiskJobResponse>>
{
    public async Task<Result<SubmitRiskJobResponse>> Handle(SubmitRiskJobCommand request, CancellationToken ct)
    {
        logger.LogInformation("Submitting risk job for ParcelId {ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null)
            return ParcelErrors.NotFound;

        if (await analysisJobRepository.HasActiveJobAsync(request.ParcelId, ct))
            return AnalysisJobErrors.AlreadyRunning;

        var createResult = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: parcel.Id,
            pythonJobId: $"pending-risk-{Guid.NewGuid():N}",
            type: AnalysisType.Risk);

        if (createResult.IsError)
            return createResult.Errors;

        await analysisJobRepository.AddAsync(createResult.Value, ct);

        var hangfireJobId = processRiskJob.Enqueue(parcel.Id, createResult.Value.Id);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        return new SubmitRiskJobResponse(
            hangfireJobId,
            parcel.Id,
            ParcelStatus.Queued.ToString(),
            DateTime.UtcNow);
    }
}
