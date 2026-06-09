using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Planora.Application.Interfaces.Jobs;

namespace Planora.Infrastructure.BackgroundJobs
{
    public sealed class ProcessTopographyJob(
    IBackgroundJobClient backgroundJobClient,
    ILogger<ProcessTopographyJob> logger)
    : IProcessTopographyJob
    {
        public string Enqueue(Guid parcelId)
        {
            var jobId =
                backgroundJobClient.Enqueue<ProcessTopographyJob>(
                    x => x.Execute(parcelId));

            return jobId;
        }

        public Task Execute(Guid parcelId)
        {
            logger.LogInformation(
                "Processing topography for ParcelId {ParcelId}",
                parcelId);

            // User Story 8
            // Call Python Service Here

            return Task.CompletedTask;
        }
    }
}
