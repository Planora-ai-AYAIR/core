using Microsoft.AspNetCore.Identity;

namespace Planora.Infrastructure.Identity;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
