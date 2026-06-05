using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Common;

public sealed class OtpRecord : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public bool IsValid => UsedAt is null && ExpiresAt > DateTime.UtcNow;
}
