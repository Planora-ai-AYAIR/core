using Planora.Application.Features.Analysis.Dtos.Realtime;
using Planora.Application.Features.Notifications.Dtos;

namespace Planora.Api.Hubs
{
    public interface INotificationClient
    {
        Task NotificationReceived(NotificationDto notification);

        Task AnalysisResultReceived(AnalysisResultEnvelope envelope);
    }
}
