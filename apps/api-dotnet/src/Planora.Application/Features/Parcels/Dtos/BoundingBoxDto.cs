using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Parcels.Dtos
{
    public sealed record BoundingBoxDto(
    double MinX,
    double MinY,
    double MaxX,
    double MaxY);
}
