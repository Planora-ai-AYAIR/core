using Planora.Domain.Parcels;

namespace Planora.Application.Interfaces.Repositories;

public interface IParcelRepository
{
    Task AddAsync(Parcel parcel, CancellationToken cancellationToken = default);
    Task<Parcel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Parcel parcel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Parcel>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid parcelId, CancellationToken cancellationToken = default);
}