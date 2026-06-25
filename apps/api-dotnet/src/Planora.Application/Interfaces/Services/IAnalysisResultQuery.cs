using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;

namespace Planora.Application.Interfaces.Services
{
    public interface IAnalysisResultQuery
    {
        Task<AggregatedAnalysisData?> GetByParcelIdAsync(Guid parcelId, CancellationToken ct);
    }
}
