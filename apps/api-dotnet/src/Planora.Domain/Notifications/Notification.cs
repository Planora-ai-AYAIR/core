using Planora.Domain.Enums;
using Planora.Domain.Shared.Abstractions;
using Planora.Domain.Shared.Results;

namespace Planora.Domain.Notifications;

public sealed class Notification : AuditableEntity
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; } = false;
    public DateTime? ReadAt { get; private set; }
    public string? Data { get; private set; } // Mapped to JSONB

    private Notification() { }

    public static Result<Notification> Create(Guid id, Guid userId, NotificationType type, string title, string message, string? data = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return NotificationErrors.TitleRequired;

        if (string.IsNullOrWhiteSpace(message))
            return NotificationErrors.MessageRequired;

        return new Notification
        {
            Id = id,
            UserId = userId,
            Type = type,
            Title = title.Trim(),
            Message = message.Trim(),
            Data = data,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}