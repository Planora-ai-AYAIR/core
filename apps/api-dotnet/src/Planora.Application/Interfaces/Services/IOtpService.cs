namespace Planora.Application.Interfaces.Services;

public interface IOtpService
{
    Task<string> GenerateAsync(Guid userId, string purpose, TimeSpan ttl, CancellationToken ct);
    Task<bool> ValidateAsync(Guid userId, string purpose, string code, CancellationToken ct);
}
