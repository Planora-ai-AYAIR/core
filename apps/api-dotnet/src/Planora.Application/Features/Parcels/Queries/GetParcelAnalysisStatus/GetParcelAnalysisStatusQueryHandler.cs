using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Parcels.Dtos.AnalysisStatus;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelAnalysisStatus;

public sealed class GetParcelAnalysisStatusQueryHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    ILogger<GetParcelAnalysisStatusQueryHandler> logger)
    : IRequestHandler<GetParcelAnalysisStatusQuery, Result<ParcelAnalysisStatusResponse>>
{
    private const string StatusPending    = "Pending";
    private const string StatusProcessing = "Processing";
    private const string StatusCompleted  = "Completed";
    private const string StatusFailed     = "Failed";

    public async Task<Result<ParcelAnalysisStatusResponse>> Handle(
        GetParcelAnalysisStatusQuery request,
        CancellationToken ct)
    {
        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null || parcel.UserId != request.UserId)
        {
            logger.LogWarning(
                $"Analysis status requested for inaccessible parcel. ParcelId: {request.ParcelId}, UserId: {request.UserId}",
                request.ParcelId, request.UserId);
            return ParcelErrors.NotFound;
        }

        // Single lightweight read of all jobs for the parcel.
        var jobs = await analysisJobRepository.GetByParcelIdAsync(request.ParcelId, ct);

        // Build per-module breakdown + aggregate.
        var modules = jobs
            .Select(j => new ModuleStatusDto(
                Type:         j.Type.ToString(),
                Status:       j.Status.ToString(),
                ErrorMessage: j.ErrorMessage,
                CompletedAt:  j.CompletedAt))
            .ToList();

        var response = new ParcelAnalysisStatusResponse(
            ParcelId:  request.ParcelId,
            Status:    ComputeAggregateStatus(jobs),
            Modules:   modules,
            UpdatedAt: jobs.Count == 0 ? null : jobs.Max(j => j.UpdatedAt ?? j.CreatedAt));

        return response;
    }

    private static string ComputeAggregateStatus(IReadOnlyList<AnalysisJob> jobs)
    {
        if (jobs.Count == 0)
            return StatusPending;

        if (jobs.Any(j => j.Status == AnalysisJobStatus.Failed))
            return StatusFailed;

        if (jobs.All(j => j.Status == AnalysisJobStatus.Completed))
            return StatusCompleted;

        return StatusProcessing;
    }
}
