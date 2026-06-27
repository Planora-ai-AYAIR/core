using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Parcels.Dtos.SubmitTopographyJob
{
    public sealed record SubmitTopographyJobRequest(Guid ParcelId);
}
