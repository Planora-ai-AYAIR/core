using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid Id)
    : IRequest<Result<Updated>>;
