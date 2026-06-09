using System.Linq.Expressions;

namespace Planora.Application.Interfaces.Services;

public interface IBackgroundJobService
{
    string Enqueue<T>(Expression<Action<T>> methodCall);
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);
    void Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);
    void AddOrUpdateRecurring<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression);
}
