using Planora.Domain.Enums;
using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Reports;

public sealed class ReportModule : AuditableEntity
{
    public Guid ReportId { get; private set; }
    public ModuleType ModuleType { get; private set; }
    public ModuleStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? OutputS3Key { get; private set; }
    public string? OutputMetadata { get; private set; } // Will be mapped to JSONB
    public int? PageCount { get; private set; }
    public long? FileSizeBytes { get; private set; }

    private ReportModule() { }

    public static ReportModule Create(Guid id, Guid reportId, ModuleType type)
    {
        return new ReportModule
        {
            Id = id,
            ReportId = reportId,
            ModuleType = type,
            Status = ModuleStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetOutput(string s3Key, int? pageCount = null, long? sizeBytes = null)
    {
        OutputS3Key = s3Key;
        PageCount = pageCount;
        FileSizeBytes = sizeBytes;
        Status = ModuleStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
