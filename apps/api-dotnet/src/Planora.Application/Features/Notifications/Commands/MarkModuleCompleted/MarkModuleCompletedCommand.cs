using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Notifications.Commands.MarkModuleCompleted
{
    public sealed record MarkModuleCompletedCommand(
        string PythonJobId,
        AnalysisType ModuleType,
        string? ResultSummary)
        : IRequest<Result<Success>>;
}
