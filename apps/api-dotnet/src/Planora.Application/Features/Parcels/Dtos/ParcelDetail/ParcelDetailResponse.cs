using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Parcels.Dtos.ParcelDetail
{
    public sealed record ParcelDetailResponse(
        Guid Id,
        string Name,
        decimal AreaHectares,
        string? Country,
        string? Governorate,
        string Status,
        string? GeojsonKey,
        DateTime CreatedAt);
}
