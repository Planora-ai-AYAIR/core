using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Helpers;
using Planora.Application.Features.Parcels.Commands.CreateParcel;
using Planora.Application.Features.Parcels.Dtos.CreateParcel;
using Planora.Domain.Shared.Results;

namespace Planora.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ParcelsController : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult> CreateParcel(
        [FromBody] CreateParcelRequest request,
        ISender sender,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
            return Problem([Error.Unauthorized("Unauthorized", "User ID not found in token.")]);

        var command = new CreateParcelCommand(
            userId,
            request.Name,
            request.GeoJson.GetRawText(),
            request.Area,
            request.AreaUnit);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            onValue: response => CreatedEnvelope(response, "Parcel created successfully"),
            onError: errors => Problem(errors));
    }
}