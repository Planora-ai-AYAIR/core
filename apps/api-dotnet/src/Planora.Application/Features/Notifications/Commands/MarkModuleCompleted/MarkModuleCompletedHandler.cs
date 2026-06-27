using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Notifications.Dtos;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Notifications;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Features.Notifications.Commands.MarkModuleCompleted
{
    public sealed class MarkModuleCompletedHandler(
        IAnalysisJobRepository analysisJobs,
        IParcelRepository parcels,
        INotificationRepository notifications,
        INotificationPublisher publisher,
        ILogger<MarkModuleCompletedHandler> logger)
        : IRequestHandler<MarkModuleCompletedCommand, Result<Success>>
    {

        public async Task<Result<Success>> Handle(MarkModuleCompletedCommand cmd, CancellationToken ct)
        {
            var job = await analysisJobs.GetByPythonJobIdAsync(cmd.PythonJobId, ct);
            if (job is null) return AnalysisJobErrors.NotFound;

            // Idempotency
            if (job.Status == AnalysisJobStatus.Completed)
            {
                logger.LogInformation(
                    "Duplicate webhook for {PythonJobId} ignored (already completed).",
                    cmd.PythonJobId);
                return Result.Success;
            }

            var transition = job.MarkAsCompleted();
            if (transition.IsError) return transition.TopError;

            var parcel = await parcels.GetByIdAsync(job.ParcelId, ct);
            if (parcel is null) return ParcelErrors.NotFound;

            var data = JsonSerializer.Serialize(new
            {
                parcelId = parcel.Id,
                moduleType = cmd.ModuleType.ToString(),
                analysisJobId = job.Id,
                link = $"/parcels/{parcel.Id}/reports/{cmd.ModuleType.ToString().ToLower()}"
            });

            var result = Notification.Create(
                id: Guid.NewGuid(),
                userId: parcel.UserId,
                type: NotificationType.ModuleCompleted,
                title: $"{cmd.ModuleType} analysis complete",
                message: $"{cmd.ModuleType} analysis complete for Parcel #{parcel.Id.ToString()[..8]}",
                data: data);

            if (result.IsError) return result.TopError;

            await analysisJobs.UpdateAsync(job, ct);
            await notifications.AddAsync(result.Value, ct);

            var dto = ToDto(result.Value);
            await publisher.PublishAsync(parcel.UserId, dto, ct);

            return Result.Success;
        }

        private static NotificationDto ToDto(Notification n) => new(
            n.Id, n.Type, n.Title, n.Message,
            Link: ExtractLink(n.Data), Data: n.Data,
            CreatedAt: n.CreatedAt, IsRead: n.IsRead);

        private static string? ExtractLink(string? data) =>
            data is null ? null : JsonDocument.Parse(data).RootElement
                .TryGetProperty("link", out var l) ? l.GetString() : null;
    }
}