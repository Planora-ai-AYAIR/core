namespace Planora.Application.Interfaces.Services;

public interface IStorageService
{
    public interface IStorageService
    {
        /// <summary>
        /// Generates a presigned URL for downloading an existing S3 object.
        /// </summary>
        Task<string> GetPreSignedUrlAsync(string s3Key, TimeSpan expiry);

        /// <summary>
        /// Uploads bytes to S3 and returns the S3 URI.
        /// </summary>
        Task<string> UploadAsync(
            byte[] data,
            string s3Key,
            string contentType,
            CancellationToken ct = default);

        // NEW: Generate presigned URL for ANY bucket (for AI assets)
        Task<string> GetPreSignedUrlAsync(string bucketName, string s3Key, TimeSpan expiry);
    }

    /// <summary>
    /// Generates a presigned URL for the given S3 key.
    /// Returns <c>null</c> if the key is null/empty or if the object does not exist in S3.
    /// Logs a warning when the object is not found.
    /// </summary>
    Task<string?> TryGetPreSignedUrlAsync(string? s3Key, TimeSpan expiry, CancellationToken ct = default);
}
