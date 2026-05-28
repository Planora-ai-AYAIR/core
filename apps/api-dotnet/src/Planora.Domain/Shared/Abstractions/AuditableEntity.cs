using System.ComponentModel.DataAnnotations;

namespace Planora.Domain.Shared.Abstractions;

public abstract class AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
