using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Entities;

public sealed class AuthAuditLog : AuditableEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}
