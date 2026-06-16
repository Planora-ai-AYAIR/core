using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Domain.Enums;

namespace Planora.Application.Features.Notifications.Dtos
{
    public record NotificationDto(
        Guid Id,
        NotificationType Type,
        string Title,
        string Message,
        string? Link,
        string? Data,
        DateTime CreatedAt,
        bool IsRead);
}
