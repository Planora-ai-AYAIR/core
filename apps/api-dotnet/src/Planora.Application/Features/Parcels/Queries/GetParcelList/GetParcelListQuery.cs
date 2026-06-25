using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Planora.Application.Features.Parcels.Dtos.ParcelList;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelList
{
    public sealed record GetParcelListQuery(Guid UserId)
    : IRequest<Result<ParcelListResponse>>;
}
