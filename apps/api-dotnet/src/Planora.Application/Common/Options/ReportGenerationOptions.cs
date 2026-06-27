using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Common.Options
{
    public sealed record ReportGenerationOptions(
        string? Language,
        string? CompanyName,
        string? ProjectName,
        bool IncludeMaps,
        bool IncludeTables,
        bool IncludeRiskMatrix,
        bool IncludeBoreholePlan,
        string? DisclaimerLevel);
}
