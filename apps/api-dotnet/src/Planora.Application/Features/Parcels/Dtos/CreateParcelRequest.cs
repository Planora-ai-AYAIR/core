using System.Text.Json;

namespace Planora.Application.Features.Parcels.Dtos;

public sealed record CreateParcelRequest(
    string Name,
    JsonElement GeoJson,
    decimal Area,
    string AreaUnit);