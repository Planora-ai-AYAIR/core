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
        configuration["AWS:BucketName"] ?? "planora-dev-bucket";

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
}
