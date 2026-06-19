using MediatR;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Analysis.Commands;

public sealed record SoilCompletedCommand : IRequest<Result<AnalysisJobProcessedResponse>>
{
    public string PythonJobId { get; init; } = string.Empty;
    public SoilResultPayload Payload { get; init; } = default!;
}