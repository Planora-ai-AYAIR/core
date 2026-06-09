using MediatR;
using Planora.Application.Features.Parcels.Dtos.CreateParcel;
using Planora.Domain.Shared.Results;

public sealed record CreateParcelCommand(
    Guid UserId,
    string Name,
    string GeoJson,
    decimal Area,
    string AreaUnit) : IRequest<Result<CreateParcelResponse>>;