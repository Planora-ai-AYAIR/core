using MediatR;
using Planora.Application.Features.Parcels.Dtos.RiskResults;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetRiskResults;

public sealed record GetRiskResultsQuery(
    Guid ParcelId
) : IRequest<Result<RiskResultsResponse>>;
