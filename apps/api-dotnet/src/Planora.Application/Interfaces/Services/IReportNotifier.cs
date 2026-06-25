using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Interfaces.Services
{
    public interface IReportNotifier
    {
        Task NotifyReportGeneratedAsync(Guid parcelId, Guid reportId, CancellationToken ct);
        Task NotifyReportFailedAsync(Guid parcelId, Guid reportId, string errorMessage, CancellationToken ct);
    }
}
