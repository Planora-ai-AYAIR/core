namespace Planora.Infrastructure.Options;

public sealed class SeedUsersOptions
{
    public const string SectionName = "SeedUsers";

    public SeedUserOptions Admin { get; init; } = new();
    public SeedUserOptions Client { get; init; } = new();
}

public sealed class SeedUserOptions
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool EmailConfirmed { get; init; } = true;
}
