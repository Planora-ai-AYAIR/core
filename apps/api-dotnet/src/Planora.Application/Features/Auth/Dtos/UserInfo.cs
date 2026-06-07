using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Domain.Enums;

namespace Planora.Application.Features.Auth.Dtos
{
    public sealed record UserInfo(
        Guid Id,
        string Email,
        string? PhoneNumber,
        string FirstName,
        string LastName,
        string? CompanyName,
        UserRole Role,
        ReportTier SubscriptionTier,
        bool EmailConfirmed,
        DateTime CreatedAt);
}
