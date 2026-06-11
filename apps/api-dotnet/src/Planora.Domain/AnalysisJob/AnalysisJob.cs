using Planora.Domain.Shared.Abstractions;
using Planora.Domain.Shared.Results;

namespace Planora.Domain.AnalysisJob;

public sealed class AnalysisJob : AuditableEntity
{
    public Guid ParcelId { get; private set; }
    public string PythonJobId { get; private set; } = string.Empty;
    public AnalysisType Type { get; private set; }
    public AnalysisJobStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private AnalysisJob() { }

    private AnalysisJob(Guid id, Guid parcelId, string pythonJobId, AnalysisType type)
    {
        Id = id;
        ParcelId = parcelId;
        PythonJobId = pythonJobId;
        Type = type;
        Status = AnalysisJobStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AnalysisJob> Create(Guid id, Guid parcelId, string pythonJobId, AnalysisType type)
    {
        if (parcelId == Guid.Empty) return AnalysisJobErrors.InvalidParcelId;
        if (string.IsNullOrWhiteSpace(pythonJobId)) return AnalysisJobErrors.InvalidPythonJobId;

        return new AnalysisJob(id, parcelId, pythonJobId.Trim(), type);
    }

    public Result<Updated> MarkAsRunning()
    {
        if (Status != AnalysisJobStatus.Pending) return AnalysisJobErrors.InvalidStatus;

        Status = AnalysisJobStatus.Running;
        UpdatedAt = DateTime.UtcNow;
        return Result.Updated;
    }

    public Result<Updated> MarkAsCompleted()
    {
        if (Status != AnalysisJobStatus.Running) return AnalysisJobErrors.InvalidStatus;

        Status = AnalysisJobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return Result.Updated;
    }

    public Result<Updated> MarkAsFailed(string errorMessage)
    {
        if (Status == AnalysisJobStatus.Completed) return AnalysisJobErrors.InvalidStatus;
        if (string.IsNullOrWhiteSpace(errorMessage)) return AnalysisJobErrors.InvalidErrorMessage;

        Status = AnalysisJobStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Updated;
    }
}