using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.DeleteParcel;

public sealed class DeleteParcelCommandHandler(
    IParcelRepository parcelRepository,
    ILogger<DeleteParcelCommandHandler> logger)
    : IRequestHandler<DeleteParcelCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(
        DeleteParcelCommand request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Deleting parcel {ParcelId} for user {UserId}",
            request.ParcelId,
            request.UserId);

        var parcel = await parcelRepository.GetByIdAsync(
            request.ParcelId,
            ct);

        if (parcel is null || parcel.UserId != request.UserId)
        {
            logger.LogWarning(
                "Parcel not found or access denied. ParcelId: {ParcelId}",
                request.ParcelId);

            return ParcelErrors.NotFound;
        }

        await parcelRepository.DeleteAsync(
            request.UserId,
            request.ParcelId,
            ct);

        logger.LogInformation(
            "Parcel {ParcelId} soft-deleted successfully",
            request.ParcelId);

        return Result.Deleted;
    }
}