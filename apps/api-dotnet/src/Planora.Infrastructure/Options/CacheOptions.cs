namespace Planora.Infrastructure.Options;

public sealed class CacheOptions
{
	public const string SectionName = "CacheOptions";

	public bool Enabled { get; set; } = true;

	public int? MaximumKeyLength { get; set; }

	public int? MaximumPayloadBytes { get; set; }

	public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

	public TimeSpan DefaultLocalCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
}

