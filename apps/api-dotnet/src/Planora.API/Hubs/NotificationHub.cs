using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Planora.Api.Hubs
{
    [Authorize]
    public sealed class NotificationHub(ILogger<NotificationHub> _logger) : Hub<INotificationClient>
    {
        public override Task OnConnectedAsync()
        {
            _logger.LogInformation(
                "SignalR connection established. UserId={UserId} ConnectionId={ConnectionId}",
                Context.UserIdentifier, Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation(
                "SignalR connection closed. UserId={UserId} ConnectionId={ConnectionId} Reason={Reason}",
                Context.UserIdentifier, Context.ConnectionId, exception?.Message);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
