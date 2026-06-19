using System.Text.Json;
using MediatR;
using Planora.Application.Features.Notifications.Dtos;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Notifications;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsHandler(
    IUser currentUser,
    INotificationRepository notifications)
    : IRequestHandler<GetMyNotificationsQuery, Result<NotificationsPageDto>>
{
    public async Task<Result<NotificationsPageDto>> Handle(
        GetMyNotificationsQuery query, CancellationToken ct)
    {
        if (currentUser.Id is not Guid userId)
            return Error.Unauthorized("Unauthorized", "User ID not found in token.");

        var items = await notifications.GetByUserIdAsync(
            userId, query.UnreadOnly, query.Take, query.Skip, ct);

        var total = await notifications.CountByUserIdAsync(
            userId, query.UnreadOnly, ct);

        var dtos = items.Select(ToDto).ToList();

        return new NotificationsPageDto(dtos, total, query.Skip, query.Take);
    }

    private static NotificationDto ToDto(Notification n) => new(
        n.Id, n.Type, n.Title, n.Message,
        Link: ExtractLink(n.Data), Data: n.Data,
        CreatedAt: n.CreatedAt, IsRead: n.IsRead);

    private static string? ExtractLink(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return null;
        try
        {
            using var doc = JsonDocument.Parse(data);
            return doc.RootElement.TryGetProperty("link", out var link)
                ? link.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
