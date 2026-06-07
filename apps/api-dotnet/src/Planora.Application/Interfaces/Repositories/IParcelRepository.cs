using Planora.Domain.Parcels;

namespace Planora.Application.Interfaces.Repositories;

public interface IParcelRepository
{
    Task AddAsync(Parcel parcel, CancellationToken cancellationToken = default);
    //Task<Parcel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}