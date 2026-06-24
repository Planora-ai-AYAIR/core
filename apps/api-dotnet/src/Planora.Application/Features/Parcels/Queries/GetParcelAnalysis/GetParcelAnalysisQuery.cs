using MediatR;
using Planora.Application.Features.Parcels.Dtos.Analysis;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelAnalysis;

public sealed record GetParcelAnalysisQuery(Guid ParcelId)
    : IRequest<Result<ParcelAnalysisResponse>>;
