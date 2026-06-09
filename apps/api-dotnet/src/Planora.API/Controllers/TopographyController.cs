using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Commands.SubmitTopographyJob;
using Planora.Application.Features.Parcels.Dtos.SubmitTopographyJob;

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
    }
}
