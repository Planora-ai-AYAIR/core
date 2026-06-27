namespace Planora.Application.Features.Parcels.Dtos.SoilResults;

public sealed class DepthProfileItem
{
    public string Depth { get; set; } = string.Empty;
    public double Sand { get; set; }
    public double Clay { get; set; }
    public string Type { get; set; } = string.Empty;
}
