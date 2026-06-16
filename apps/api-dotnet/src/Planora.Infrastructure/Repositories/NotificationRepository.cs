using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Notifications;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Repositories
{
    public sealed class NotificationRepository(PlanoraDbContext context) : INotificationRepository
    {
        public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
            Guid userId, bool unreadOnly, int take, int skip, CancellationToken ct)
        {
            var query = context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            if (unreadOnly) query = query.Where(n => !n.IsRead);
            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip).Take(take)
                .ToListAsync(ct);
        }
        public Task<int> CountByUserIdAsync(Guid userId, bool unreadOnly, CancellationToken ct)
        {
            var query = context.Notifications.Where(n => n.UserId == userId);
            if (unreadOnly) query = query.Where(n => !n.IsRead);
            return query.CountAsync(ct);
        }

        public Task<Notification?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct) =>
            context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

        public async Task AddAsync(Notification n, CancellationToken ct)
        {
            await context.Notifications.AddAsync(n, ct);
            await context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Notification n, CancellationToken ct)
        {
            context.Notifications.Update(n);
            await context.SaveChangesAsync(ct);
        }
    }
}
