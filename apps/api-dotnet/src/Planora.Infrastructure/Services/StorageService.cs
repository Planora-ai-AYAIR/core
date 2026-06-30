using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Planora.Application.Interfaces.Services;

namespace Planora.Infrastructure.Services;

public sealed class StorageService(
    IAmazonS3 s3Client,
    IConfiguration configuration,
    ILogger<StorageService> logger) : IStorageService
{
    private readonly string _bucketName =
        Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")
        ?? configuration["AWS:BucketName"]
        ?? "planora-ai-production-lq49e8";

    public async Task<string> GetPreSignedUrlAsync(string s3Key, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        return await s3Client.GetPreSignedURLAsync(request);
    }
    public async Task<string> UploadAsync(
            byte[] data,
            string s3Key,
            string contentType,
            CancellationToken ct = default)
    {
        using var stream = new MemoryStream(data);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.Private
        };

        await s3Client.PutObjectAsync(request, ct);

        return $"s3://{_bucketName}/{s3Key}";
    }

    // ANY bucket (AI assets)
    public async Task<string> GetPreSignedUrlAsync(string bucketName, string s3Key, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = s3Key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        return await s3Client.GetPreSignedURLAsync(request);
    }

    public async Task<string?> TryGetPreSignedUrlAsync(string? s3Key, TimeSpan expiry, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(s3Key))
            return null;

        try
        {
            await s3Client.GetObjectMetadataAsync(_bucketName, s3Key, ct);
        }
        catch (AmazonS3Exception ex) when (
            ex.StatusCode == HttpStatusCode.NotFound ||
            ex.StatusCode == HttpStatusCode.Forbidden)
        {

            logger.LogWarning(
                "S3 asset unavailable (HTTP {Status}); returning null. Bucket={Bucket}, Key={Key}",
                ex.StatusCode, _bucketName, s3Key);
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex,
                "Unexpected S3 error resolving asset; returning null. Bucket={Bucket}, Key={Key}",
                _bucketName, s3Key);
            return null;
        }

        return await GetPreSignedUrlAsync(s3Key, expiry);
    }
}

