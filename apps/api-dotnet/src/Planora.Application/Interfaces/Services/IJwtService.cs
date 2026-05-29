namespace Planora.Application.Interfaces.Services;

public interface IJwtService
{
	(string Token, DateTime ExpiresAt) GenerateAccessToken(
		Guid userId,
		string email,
		IReadOnlyList<string> roles);

	TimeSpan GetRefreshTokenLifetime();
}
