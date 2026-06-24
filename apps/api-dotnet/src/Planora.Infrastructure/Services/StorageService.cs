using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Planora.Application.Interfaces.Services;

namespace Planora.Infrastructure.Services;

public sealed class StorageService(
    IAmazonS3 s3Client,
    IConfiguration configuration) : IStorageService
{
    private readonly string _bucketName =
        Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")
        ?? configuration["AWS:BucketName"]
        ?? "planora-ai-production";

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
}