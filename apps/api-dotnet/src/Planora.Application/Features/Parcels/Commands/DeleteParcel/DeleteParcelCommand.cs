using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.DeleteParcel;

public sealed record DeleteParcelCommand(
    Guid ParcelId,
    Guid UserId)
    : IRequest<Result<Deleted>>;