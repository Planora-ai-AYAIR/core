using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Domain.Enums;
using Planora.Domain.Shared.Abstractions;
using Planora.Domain.Shared.Results;

namespace Planora.Domain.Reports
{
    public sealed class Report : AuditableEntity
    {
        public Guid ParcelId { get; private set; }
        public Guid UserId { get; private set; } // Denormalized [cite: 1789]
        public Guid? PaymentId { get; private set; } // Nullable for free trial [cite: 1793]
        public string? HangfireJobId { get; private set; }
        public ReportStatus Status { get; private set; }
        public ReportTier Tier { get; private set; }
        public int? EstimatedDurationMinutes { get; private set; }
        public DateTime? ProcessingStartedAt { get; private set; }
        public DateTime? ProcessingCompletedAt { get; private set; }
        public string? ErrorMessage { get; private set; }

        // Navigation Properties
        private readonly List<ReportModule> _modules = new();
        public IReadOnlyCollection<ReportModule> Modules => _modules.AsReadOnly();

        private readonly List<ReportFile> _files = new();
        public IReadOnlyCollection<ReportFile> Files => _files.AsReadOnly();

        private Report() { }

        private Report(Guid id, Guid parcelId, Guid userId, ReportTier tier, int? estimatedDuration)
        {
            Id = id;
            ParcelId = parcelId;
            UserId = userId;
            Tier = tier;
            Status = ReportStatus.PendingPayment;
            EstimatedDurationMinutes = estimatedDuration;
            CreatedAt = DateTime.UtcNow;
        }

        public static Result<Report> Create(Guid id, Guid parcelId, Guid userId, ReportTier tier, int? estimatedDuration = null)
        {
            if (estimatedDuration.HasValue && estimatedDuration <= 0)
                return ReportErrors.InvalidEstimatedDuration;

            return new Report(id, parcelId, userId, tier, estimatedDuration);
        }

        public void MarkAsQueued(string hangfireJobId, Guid? paymentId = null)
        {
            Status = ReportStatus.Queued;
            HangfireJobId = hangfireJobId;
            PaymentId = paymentId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsProcessing()
        {
            Status = ReportStatus.Processing;
            ProcessingStartedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsCompleted()
        {
            Status = ReportStatus.Completed;
            ProcessingCompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsFailed(string errorMessage)
        {
            Status = ReportStatus.Failed;
            ErrorMessage = errorMessage;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddModule(ReportModule module)
        {
            _modules.Add(module);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddFile(ReportFile file)
        {
            _files.Add(file);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
