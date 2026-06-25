using MediatR;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Reports.Commands.SubmitPdfJob;

public sealed record SubmitReportCommand(
    Guid ParcelId,
    string? Language,
    string? CompanyName,
    string? ProjectName,
    bool IncludeMaps,
    bool IncludeTables,
    bool IncludeRiskMatrix,
    bool IncludeBoreholePlan,
    string? DisclaimerLevel
) : IRequest<Result<SubmitReportResponse>>;