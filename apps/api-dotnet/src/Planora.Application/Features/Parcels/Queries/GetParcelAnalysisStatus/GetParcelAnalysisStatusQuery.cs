using MediatR;
using Planora.Application.Features.Parcels.Dtos.AnalysisStatus;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelAnalysisStatus;

public sealed record GetParcelAnalysisStatusQuery(Guid ParcelId, Guid UserId)
    : IRequest<Result<ParcelAnalysisStatusResponse>>;
