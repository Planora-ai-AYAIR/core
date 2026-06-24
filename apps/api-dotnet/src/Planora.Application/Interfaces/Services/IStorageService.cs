using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Interfaces.Services
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
}
