using MediatR;
using Planora.Application.Features.Parcels.Dtos.RefreshAssets;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.RefreshAssets;

public sealed record RefreshParcelAssetsCommand(Guid ParcelId, Guid UserId)
    : IRequest<Result<RefreshAssetsResponse>>;
