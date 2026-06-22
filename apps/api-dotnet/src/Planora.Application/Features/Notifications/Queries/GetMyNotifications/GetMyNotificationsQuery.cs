using MediatR;
using Planora.Application.Features.Notifications.Dtos;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(
    bool UnreadOnly,
    int Take,
    int Skip)
    : IRequest<Result<NotificationsPageDto>>;
