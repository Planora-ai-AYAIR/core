using MediatR;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Analysis.Commands.TopographyCompleted;

public sealed record TopographyCompletedCommand : IRequest<Result<AnalysisJobProcessedResponse>>
{
    public string PythonJobId { get; init; } = string.Empty;
    public TopographyResultPayload Payload { get; init; } = default!;
}