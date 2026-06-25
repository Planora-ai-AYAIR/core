using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Parcels;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Persistence.Repositories;

public sealed class ParcelRepository(PlanoraDbContext context) : IParcelRepository
{
    public async Task AddAsync(Parcel parcel, CancellationToken ct = default)
    {
        await context.Parcels.AddAsync(parcel, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Parcel?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Parcels.FindAsync(id, ct);

    public async Task UpdateAsync(Parcel parcel, CancellationToken ct = default)
    {
        context.Parcels.Update(parcel);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Parcel>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Parcels
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, Guid parcelId, CancellationToken ct = default)
    {
        var parcel = await context.Parcels.FindAsync(parcelId, ct);
        if (parcel is not null)
        {
            parcel.DeletedAt = DateTime.UtcNow;
            parcel.DeletedBy = userId;
            await context.SaveChangesAsync(ct);
        }
    }

}