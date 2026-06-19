using MediatR;
using Planora.Application.Features.Analysis.Webhooks.Payloads;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Analysis.Webhooks;

public sealed record RiskCompletedCommand : IRequest<Result<AnalysisJobProcessedResponse>>
{
    public string PythonJobId { get; init; } = string.Empty;
    public RiskResultPayload Payload { get; init; } = default!;
}