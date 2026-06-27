using FluentValidation;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Planora.Application.Features.Parcels.Commands.CreateParcel;

public sealed class CreateParcelValidator : AbstractValidator<CreateParcelCommand>
{
    private static readonly string[] ValidUnits = { "m2", "hectares", "acres" };

    public CreateParcelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("Parcel.Name.INVALID_CLIENT_NAME")
            .WithMessage("Client name is required.");

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .WithErrorCode("Parcel.Name.INVALID_CLIENT_NAME")
            .WithMessage("Client name must not exceed 200 characters.");

        RuleFor(x => x.AreaUnit)
            .Must(u => ValidUnits.Contains(u.ToLowerInvariant()))
            .WithErrorCode("Parcel.AreaUnit.INVALID_UNIT")
            .WithMessage("Invalid area unit. Allowed values: m2, hectares, acres.");

        RuleFor(x => x.Area)
            .Must(BeAtLeastFiveHectares)
            .WithErrorCode("Parcel.Area.PARCEL_TOO_SMALL")
            .WithMessage("Parcel area must be at least 5 hectares (50,000 m²).");

        RuleFor(x => x.GeoJson)
            .Must(BeValidPolygon)
            .WithErrorCode("Parcel.GeoJson.INVALID_GEOMETRY")
            .WithMessage("Invalid polygon geometry provided.");
    }

    private static bool BeAtLeastFiveHectares(CreateParcelCommand cmd, decimal area)
    {

        if (!ValidUnits.Contains(cmd.AreaUnit?.ToLowerInvariant() ?? string.Empty))
            return true;

        var hectares = cmd.AreaUnit.ToLowerInvariant() switch
        {
            "m2" => area / 10_000m,
            "hectares" => area,
            "acres" => area * 0.404686m,
            _ => area
        };
        return hectares >= 5.0m;
    }

    private static bool BeValidPolygon(string geoJson)
    {
        try
        {
            var geometry = new GeoJsonReader().Read <Geometry> (geoJson);
            if (geometry is not Polygon polygon)
                return false;

            if (!polygon.IsValid || polygon.IsEmpty)   
                return false;

            var env = polygon.EnvelopeInternal;
            if (double.IsNaN(env.MinX) || double.IsInfinity(env.MinX))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}