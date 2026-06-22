using MediatR;
using Planora.Application.Features.Parcels.Dtos.BoreholeResults;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetBoreholeResults;

public sealed record GetBoreholeResultsQuery(
    Guid ParcelId
) : IRequest<Result<BoreholeResultsResponse>>;
