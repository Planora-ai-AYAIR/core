using MediatR;
using Planora.Application.Features.Parcels.Dtos.SubmitRiskJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitRiskJob;

public sealed record SubmitRiskJobCommand(Guid ParcelId)
    : IRequest<Result<SubmitRiskJobResponse>>;
