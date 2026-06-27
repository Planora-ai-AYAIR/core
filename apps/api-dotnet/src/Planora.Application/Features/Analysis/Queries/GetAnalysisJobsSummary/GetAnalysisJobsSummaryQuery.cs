using MediatR;
using Planora.Application.Features.Analysis.Dtos.AnalysisJobsSummary;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Analysis.Queries.GetAnalysisJobsSummary;

public sealed record GetAnalysisJobsSummaryQuery(Guid UserId)
    : IRequest<Result<AnalysisJobsSummaryResponse>>;
