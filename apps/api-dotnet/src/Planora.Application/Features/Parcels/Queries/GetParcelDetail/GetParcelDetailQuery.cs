using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Planora.Application.Features.Parcels.Dtos.ParcelDetail;
using Planora.Application.Features.Parcels.Dtos.ParcelList;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelDetail
{
    public sealed record GetParcelDetailQuery(
    Guid ParcelId,
    Guid UserId)
    : IRequest<Result<ParcelDetailResponse>>;
}
