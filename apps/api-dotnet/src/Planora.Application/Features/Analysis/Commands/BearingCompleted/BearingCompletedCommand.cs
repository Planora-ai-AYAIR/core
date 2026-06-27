using MediatR;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Analysis.Commands.BearingCompleted;

public sealed record BearingCompletedCommand : IRequest<Result<AnalysisJobProcessedResponse>>
{
    public string PythonJobId { get; init; } = string.Empty;
    public BearingResultPayload Payload { get; init; } = default!;
}
