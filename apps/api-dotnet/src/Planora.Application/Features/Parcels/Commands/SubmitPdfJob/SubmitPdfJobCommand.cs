using MediatR;
using Planora.Application.Features.Parcels.Dtos.SubmitPdfJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitPdfJob;

public sealed record SubmitPdfJobCommand(
    Guid ParcelId,
    Guid ReportId,
    string? Language = "en",
    bool IncludeMaps = true,
    bool IncludeTables = true,
    bool IncludeRiskMatrix = true,
    string? DisclaimerLevel = "full",
    string? CompanyName = null,
    string? ProjectName = null)
    : IRequest<Result<SubmitPdfJobResponse>>;
