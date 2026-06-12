using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Interfaces.Services
{
    public interface IStorageService
    {
        Task<string> GetPreSignedUrlAsync(string s3Key, TimeSpan expiry);
    }
}
