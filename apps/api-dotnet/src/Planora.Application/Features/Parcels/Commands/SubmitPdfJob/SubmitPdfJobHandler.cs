using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
using Planora.Application.Features.Parcels.Dtos.SubmitPdfJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitPdfJob;

public sealed class SubmitPdfJobHandler(
    IParcelRepository parcelRepository,
    IHybridCacheService cacheService,
    IProcessPdfJob processPdfJob,
    ILogger<SubmitPdfJobHandler> logger)
    : IRequestHandler<SubmitPdfJobCommand, Result<SubmitPdfJobResponse>>
{
    public async Task<Result<SubmitPdfJobResponse>> Handle(SubmitPdfJobCommand request, CancellationToken ct)
    {
        logger.LogInformation("Submitting PDF job for ParcelId {ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);

        if (parcel is null)
        {
            return ParcelErrors.NotFound;
        }

        var processPdfRequest = new ProccessPdfJobAiRequest(
            ParcelId: parcel.Id,
            ReportId: request.ReportId,
            Language: request.Language,
            IncludeMaps: request.IncludeMaps,
            IncludeTables: request.IncludeTables,
            IncludeRiskMatrix: request.IncludeRiskMatrix,
            DisclaimerLevel: request.DisclaimerLevel,
            CompanyName: request.CompanyName,
            ProjectName: request.ProjectName
        );

        var jobId = processPdfJob.Enqueue(processPdfRequest);

        return new SubmitPdfJobResponse(jobId, parcel.Id, ParcelStatus.Queued.ToString(), DateTime.UtcNow);
    }
}
