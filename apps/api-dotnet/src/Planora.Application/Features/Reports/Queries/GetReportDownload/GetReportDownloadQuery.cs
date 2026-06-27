using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Planora.Application.Features.Reports.Dtos.GetReportDownload;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Reports.Queries.GetReportDownload
{
    public sealed record GetReportDownloadQuery(Guid ReportId)
    : IRequest<Result<GetReportDownloadResponse>>;
}
