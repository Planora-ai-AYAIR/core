namespace Planora.Application.Interfaces.Services;

public interface IStorageService
{
    Task<string> GetPreSignedUrlAsync(string s3Key, TimeSpan expiry);

    /// <summary>
    /// Generates a presigned URL for the given S3 key.
    /// Returns <c>null</c> if the key is null/empty or if the object does not exist in S3.
    /// Logs a warning when the object is not found.
    /// </summary>
    Task<string?> TryGetPreSignedUrlAsync(string? s3Key, TimeSpan expiry, CancellationToken ct = default);
}
