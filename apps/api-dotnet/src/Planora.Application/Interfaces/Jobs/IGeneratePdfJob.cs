using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Application.Common.Options;

namespace Planora.Application.Interfaces.Jobs
{
    public interface IGeneratePdfJob
    {
        string Enqueue(Guid reportId, ReportGenerationOptions options);
    }
}
