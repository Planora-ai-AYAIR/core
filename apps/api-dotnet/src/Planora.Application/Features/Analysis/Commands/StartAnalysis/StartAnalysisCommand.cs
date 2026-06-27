using MediatR;
using Planora.Application.Features.Analysis.Dtos.StartAnalysis;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Analysis.Commands.StartAnalysis;

public sealed record StartAnalysisCommand(
    Guid ParcelId,
    AnalysisOptionsDto Options) : IRequest<Result<StartAnalysisResponse>>;
