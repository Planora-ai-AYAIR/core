using Microsoft.AspNetCore.Identity;
using Planora.Domain.Shared.Abstractions.Interfaces;
using Planora.Domain.Enums;

namespace Planora.Infrastructure.Identity;

public class User : IdentityUser<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    // Custom Properties from Schema
    public string FirstName { get; set; } = string.Empty; // Note: Schema specifies 'full_name', we can split it or combine it in a DTO
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Client; // Default 'Client'

    public bool IsBanned { get; set; } = false;
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }

    public ReportTier SubscriptionTier { get; set; } = ReportTier.Free; // Default 'Free'
    public bool FreeTrialUsed { get; set; } = false;

    public DateTime? TermsAcceptedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LoginCount { get; set; } = 0;

    // IAuditableEntity Implementation
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletableEntity Implementation
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}