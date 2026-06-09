using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
