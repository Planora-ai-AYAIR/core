using MediatR;
using Planora.Application.Features.Parcels.Dtos;
using Planora.Domain.Shared.Results;

public sealed record CreateParcelCommand(
    Guid UserId,
    string Name,
    string GeoJson,
    decimal Area,
    string AreaUnit) : IRequest<Result<CreateParcelResponse>>;