using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Planora.Application.Features.Parcels.Dtos.SubmitTopographyJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitTopographyJob
{
    public sealed record SubmitTopographyJobCommand(Guid ParcelId)
    : IRequest<Result<SubmitTopographyJobResponse>>;
}
