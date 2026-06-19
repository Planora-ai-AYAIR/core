using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Analysis.Webhooks;

public sealed record AnalysisFailedCommand : IRequest<Result<AnalysisJobProcessedResponse>>
{
    public string PythonJobId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}