using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Commands.SubmitPdfJob;
using Planora.Application.Features.Parcels.Dtos.PdfReport;
using Planora.Application.Features.Reports.Commands.SubmitPdfJob;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;
using Planora.Application.Features.Reports.Queries.GetPdfReport;

namespace Planora.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : BaseApiController
{

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

    [HttpPost("/api/parcels/{parcelId:guid}/reports")]
    public async Task<ActionResult> SubmitJob(
        Guid parcelId,
        [FromBody] SubmitReportRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new SubmitReportCommand(
            parcelId,
            request.Language,
            request.CompanyName,
            request.ProjectName,
            request.IncludeMaps,
            request.IncludeTables,
            request.IncludeRiskMatrix,
            request.IncludeBoreholePlan,
            request.DisclaimerLevel);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            response => AcceptedEnvelope(
                response,
                "PDF report generation job accepted for processing"),
            errors => Problem(errors));
    }
}
