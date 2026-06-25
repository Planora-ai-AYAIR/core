using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Parcels.Dtos.ParcelList
{
    public sealed record ParcelSummaryDto(
    Guid Id,
    string Name,
    decimal AreaHectares,
    string Status,
    DateTime CreatedAt);

    public sealed record ParcelListResponse(
        IReadOnlyList<ParcelSummaryDto> Parcels);
}
