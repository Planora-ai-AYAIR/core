using Hangfire;
using Planora.Application.Interfaces.Services;
using System.Linq.Expressions;

namespace Planora.Infrastructure.Services;

public class BackgroundJobService(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager) : IBackgroundJobService
{
    public string Enqueue<T>(Expression<Action<T>> methodCall) =>
        backgroundJobClient.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) =>
        backgroundJobClient.Enqueue(methodCall);

    public void Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) =>
        backgroundJobClient.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression) =>
        recurringJobManager.AddOrUpdate(jobId, methodCall, cronExpression);
}