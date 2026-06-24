using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Application.Features.Notifications.Dtos;

namespace Planora.Application.Interfaces.Services
{
    public interface INotificationPublisher
    {
        Task PublishAsync(Guid userId, NotificationDto notification, CancellationToken ct);
    Task PublishToGroupAsync(string groupName, NotificationDto notification, CancellationToken ct);
    }
}
