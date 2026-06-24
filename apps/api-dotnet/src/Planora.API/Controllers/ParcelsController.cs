using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Helpers;
using Planora.Application.Features.Parcels.Commands.CreateParcel;
using Planora.Application.Features.Parcels.Dtos.CreateParcel;
using Planora.Application.Features.Parcels.Queries.GetParcelAnalysisStatus;
using Planora.Application.Features.Parcels.Queries.GetParcelAnalysis;
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
    
    [HttpGet("{parcelId:guid}/analysis-status")]
    public async Task<ActionResult> GetParcelAnalysisStatus(
        Guid parcelId,
        ISender sender,
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
            return Problem([Error.Unauthorized("Unauthorized", "User ID not found in token.")]);

        var query = new GetParcelAnalysisStatusQuery(parcelId, userId);
        var result = await sender.Send(query, ct);

        return result.Match<ActionResult>(
            onValue:  response => OkEnvelope(response, "Parcel analysis status retrieved successfully"),
            onError:  errors   => Problem(errors));
    }

    [HttpGet("{parcelId:guid}/analysis")]
    public async Task<ActionResult> GetParcelAnalysis(
        Guid parcelId,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetParcelAnalysisQuery(parcelId);
        var result = await sender.Send(query, ct);

        return result.Match<ActionResult>(
            onValue:  response => OkEnvelope(response, "Parcel analysis assets retrieved successfully"),
            onError:  errors   => Problem(errors));
    }
}