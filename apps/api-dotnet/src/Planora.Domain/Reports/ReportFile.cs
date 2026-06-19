using Planora.Domain.Enums;
using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Reports;

public sealed class ReportFile : AuditableEntity
{
    public Guid ReportId { get; private set; }
    public FileType FileType { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string S3Key { get; private set; } = string.Empty;
    public long? FileSizeBytes { get; private set; }
    public string ContentType { get; private set; } = string.Empty;

    private ReportFile() { }

    public static ReportFile Create(Guid id, Guid reportId, FileType type, string fileName, string s3Key, string contentType, long? sizeBytes)
    {
        return new ReportFile
        {
            Id = id,
            ReportId = reportId,
            FileType = type,
            FileName = fileName,
            S3Key = s3Key,
            ContentType = contentType,
            FileSizeBytes = sizeBytes,
            CreatedAt = DateTime.UtcNow
        };
    }
}