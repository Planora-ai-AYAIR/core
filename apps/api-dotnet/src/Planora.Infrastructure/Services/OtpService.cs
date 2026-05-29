using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Entities;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Services;

public sealed class OtpService : IOtpService
{
    private readonly PlanoraDbContext _dbContext;

    public OtpService(PlanoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateAsync(Guid userId, string purpose, TimeSpan ttl, CancellationToken ct)
    {
        var code = GenerateCode();

        var record = new OtpRecord
        {
            UserId = userId,
            Purpose = purpose,
            Code = code,
            ExpiresAt = DateTime.UtcNow.Add(ttl),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.OtpRecords.Add(record);
        await _dbContext.SaveChangesAsync(ct);

        return code;
    }

    public async Task<bool> ValidateAsync(Guid userId, string purpose, string code, CancellationToken ct)
    {
        var record = await _dbContext.OtpRecords
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Purpose == purpose &&
                x.Code == code &&
                x.UsedAt == null &&
                x.ExpiresAt > DateTime.UtcNow, ct);

        if (record is null)
        {
            return false;
        }

        record.UsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    private static string GenerateCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return value.ToString("D6");
    }
}
