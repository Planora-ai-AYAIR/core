using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;

namespace Planora.Application.Interfaces.Services
{
    public interface IPdfGeneratorService
    {
        Task<byte[]> GenerateAsync(ReportPdfData data, CancellationToken ct);
    }
}
