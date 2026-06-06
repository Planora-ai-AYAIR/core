using Planora.Domain.Shared.Results;

namespace Planora.Domain.Parcels;

public static class ParcelErrors
{
    public static readonly Error InvalidName = Error.Validation("Parcel.Name.Required", "Parcel name cannot be empty.");
    public static readonly Error AreaTooSmall = Error.Validation("Parcel.Area.TooSmall", "Parcel area must be at least 0.5 hectares.");
    public static readonly Error InvalidBoundary = Error.Validation("Parcel.Boundary.Invalid", "Invalid polygon boundary provided.");
}