using MediatR;
using Planora.Application.Features.Parcels.Dtos.SoilResults;
using Planora.Application.Features.Parcels.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetSoilResults;

public sealed record GetSoilResultsQuery(
    Guid ParcelId,
    string? Depth = null
) : IRequest<Result<SoilResultsResponse>>;
