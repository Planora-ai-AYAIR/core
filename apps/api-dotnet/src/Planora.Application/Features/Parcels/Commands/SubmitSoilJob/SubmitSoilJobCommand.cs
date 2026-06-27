using MediatR;
using Planora.Application.Features.Parcels.Dtos.SubmitSoilJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitSoilJob;

public sealed record SubmitSoilJobCommand(Guid ParcelId)
    : IRequest<Result<SubmitSoilJobResponse>>;
