using Planora.Application.Features.Notifications.Dtos;

namespace Planora.Application.Features.Notifications.Dtos;

public sealed record NotificationsPageDto(
    IReadOnlyList<NotificationDto> Items,
    int Total,
    int Skip,
    int Take);
