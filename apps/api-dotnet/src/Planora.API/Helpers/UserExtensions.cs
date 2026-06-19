using System.Security.Claims;

namespace Planora.Api.Helpers;

public static class UserExtinsions
{
	public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId)
	{
		userId = Guid.Empty;
		var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
		return Guid.TryParse(idValue, out userId);
	}
}