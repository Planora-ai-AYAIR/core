
namespace Planora.Application.Features.Parcels.Dtos.CreateParcel
{
    public sealed record CreateParcelResponse(
        Guid ParcelId,
        string Name,
        BoundingBoxDto BoundingBox,
        BoundingBoxDto BufferedBoundingBox,
        decimal Area,
        DateTime CreatedAt);
}
