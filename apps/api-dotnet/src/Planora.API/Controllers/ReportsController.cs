using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Commands.SubmitPdfJob;
using Planora.Application.Features.Parcels.Dtos.PdfReport;
using Planora.Application.Features.Reports.Commands.SubmitPdfJob;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;
using Planora.Application.Features.Reports.Queries.GetPdfReport;
using Planora.Application.Features.Reports.Queries.GetReportDownload;

namespace Planora.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : BaseApiController
{

    
    [HttpGet("{reportId:guid}")]
    public async Task<ActionResult> GetReportDownload(
        Guid reportId,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetReportDownloadQuery(reportId);
        var result = await sender.Send(query, ct);

        return result.Match<ActionResult>(
            response => OkEnvelope(response, "Presigned URL generated"),
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
