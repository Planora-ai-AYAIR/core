using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Domain.Notifications;

namespace Planora.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<IReadOnlyList<Notification>> GetByUserIdAsync(
            Guid userId, bool unreadOnly, int take, int skip, CancellationToken ct);
        Task<int> CountByUserIdAsync(Guid userId, bool unreadOnly, CancellationToken ct);
        Task<Notification?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct);
        Task AddAsync(Notification notification, CancellationToken ct);
        Task UpdateAsync(Notification notification, CancellationToken ct);
    }
}
