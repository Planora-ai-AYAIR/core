using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Planora.Application.Features.Notifications.Queries.GetMyNotifications;

namespace Planora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class NotificationsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult> GetMine(
        ISender sender,
        CancellationToken ct,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int take = 20,
        [FromQuery] int skip = 0)
    {
        var result = await sender.Send(
            new GetMyNotificationsQuery(unreadOnly, take, skip), ct);

        return result.Match<ActionResult>(
            onValue: page => OkEnvelope(page, "Notifications retrieved successfully"),
            onError: errors => Problem(errors));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult> MarkAsRead(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new MarkNotificationAsReadCommand(id), ct);

        return result.Match<ActionResult>(
            onValue: _ => OkEnvelope<object?>(null, "Notification marked as read"),
            onError: errors => Problem(errors));
    }
}
