using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Commands.SubmitPdfJob;
using Planora.Application.Features.Parcels.Dtos.PdfReport;
using Planora.Application.Features.Parcels.Dtos.SubmitPdfJob;
using Planora.Application.Features.Parcels.Queries.GetPdfReport;

namespace Planora.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : BaseApiController
{
    [HttpPost("jobs")]
    public async Task<ActionResult> SubmitJob(
        [FromBody] SubmitPdfJobRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new SubmitPdfJobCommand(
            request.ParcelId,
            request.ReportId,
            request.Language,
            request.IncludeMaps,
            request.IncludeTables,
            request.IncludeRiskMatrix,
            request.DisclaimerLevel,
            request.CompanyName,
            request.ProjectName);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            response => AcceptedEnvelope(
                response,
                "PDF report generation job accepted for processing"),
            errors => Problem(errors));
    }

    [HttpGet("{parcelId:guid}/pdf")]
    public async Task<ActionResult> GetPdfReport(
        Guid parcelId,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetPdfReportQuery(parcelId);
        var result = await sender.Send(query, ct);

        return result.Match<ActionResult>(
            response => OkEnvelope(response, "PDF report retrieved successfully"),
            errors => Problem(errors));
    }
}
