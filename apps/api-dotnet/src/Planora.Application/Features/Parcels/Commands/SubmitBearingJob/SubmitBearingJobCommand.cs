using MediatR;
using Planora.Application.Features.Parcels.Dtos.SubmitBearingJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitBearingJob;

public sealed record SubmitBearingJobCommand(Guid ParcelId)
    : IRequest<Result<SubmitBearingJobResponse>>;
