using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Commands.SubmitTopographyJob;
using Planora.Application.Features.Parcels.Dtos.SubmitTopographyJob;
using Planora.Application.Features.Parcels.Queries.GetTopographyResults;
namespace Planora.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TopographyController : BaseApiController
    {
        [HttpPost("jobs")]
        public async Task<ActionResult> SubmitJob(
            [FromBody] SubmitTopographyJobRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new SubmitTopographyJobCommand(request.ParcelId);

            var result = await sender.Send(command, ct);

            return result.Match<ActionResult>(
                response => AcceptedEnvelope(
                    response,
                    "Topography job accepted for processing"),
                errors => Problem(errors));
        }

        [HttpGet("{parcelId:guid}")]
        public async Task<ActionResult> GetResults(
            Guid parcelId,
            [FromQuery] bool includeTiles,
            [FromQuery] string format = "json",
            ISender sender = null!,
            CancellationToken ct = default)
        {
            var query = new GetTopographyResultsQuery(parcelId, includeTiles, format.ToLower());
            var result = await sender.Send(query, ct);

            return result.Match<ActionResult>(
                response => OkEnvelope(response, "Topography results retrieved successfully"),
                errors => Problem(errors));
        }
    }
}
