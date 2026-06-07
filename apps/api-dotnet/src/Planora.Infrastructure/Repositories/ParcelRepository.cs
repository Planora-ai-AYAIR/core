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
}