using MediatR;
using Planora.Application.Features.Parcels.Dtos.SubmitBoreholeJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitBoreholeJob;

public sealed record SubmitBoreholeJobCommand(Guid ParcelId)
    : IRequest<Result<SubmitBoreholeJobResponse>>;
