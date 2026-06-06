using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Domain.Shared.Results;

namespace Planora.Domain.Reports
{
    public static class ReportErrors
    {
        public static readonly Error InvalidEstimatedDuration = Error.Validation("Report.Duration.Invalid", "Estimated duration must be positive.");
        public static readonly Error ModuleNotFound = Error.NotFound("Report.Module.NotFound", "The requested module was not found in this report.");
    }
}
