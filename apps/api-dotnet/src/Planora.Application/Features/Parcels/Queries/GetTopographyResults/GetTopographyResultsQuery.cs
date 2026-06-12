using MediatR;
using Planora.Application.Features.Parcels.Dtos.TopographyResults;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetTopographyResults;

public record GetTopographyResultsQuery(
    Guid ParcelId,
    bool IncludeTiles = true,
    string Format = "json"
) : IRequest<Result<TopographyResultsResponse>>;
