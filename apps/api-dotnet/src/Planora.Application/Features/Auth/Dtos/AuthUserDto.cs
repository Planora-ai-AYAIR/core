using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Auth.Dtos
{
    public sealed record AuthUserDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        IList<string> Roles,
        IList<Claim> Claims,
        bool EmailConfirmed);
}
