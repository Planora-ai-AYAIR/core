using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Application.Features.Analysis.Dtos.Realtime;
using Planora.Application.Features.Notifications.Dtos;

namespace Planora.Application.Interfaces.Services
{
    public interface INotificationPublisher
    {
        Task PublishAsync(Guid userId, NotificationDto notification, CancellationToken ct);
    Task PublishToGroupAsync(string groupName, NotificationDto notification, CancellationToken ct);

        /// <summary>
        /// Broadcasts a completed analysis result (full typed payload) to the
        /// <c>parcel:{parcelId}</c> SignalR group for instant UI rendering.
        /// </summary>
        Task PublishAnalysisResultAsync(Guid parcelId, AnalysisResultEnvelope envelope, CancellationToken ct);
    }
}
