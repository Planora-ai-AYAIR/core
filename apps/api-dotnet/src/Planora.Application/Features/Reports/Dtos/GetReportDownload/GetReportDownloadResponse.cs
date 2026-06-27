using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Reports.Dtos.GetReportDownload
{
    public sealed record GetReportDownloadResponse(
        Guid ReportJobId,
        Guid ParcelId,
        string Status,
        string DownloadUrl,
        DateTime ExpiresAt,
        string Filename,
        long? SizeBytes,
        int? PageCount);
}
