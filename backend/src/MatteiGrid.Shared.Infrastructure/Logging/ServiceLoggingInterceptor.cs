using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace MatteiGrid.Shared.Infrastructure.Logging;

/// <summary>
/// AOP Interceptor that automatically logs method entry and exit for registered services,
/// measures execution duration in milliseconds, sanitizes parameters, and logs caught errors.
/// </summary>
public class ServiceLoggingInterceptor : IInterceptor
{
    private readonly ILogger<ServiceLoggingInterceptor> _logger;
    private static readonly ConcurrentDictionary<Type, MethodInfo> TaskInterceptorMethods = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> ValueTaskInterceptorMethods = new();

    public ServiceLoggingInterceptor(ILogger<ServiceLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var targetType = invocation.TargetType?.Name ?? invocation.Method.DeclaringType?.Name ?? "Service";
        var methodName = invocation.Method.Name;
        var sanitizedArgs = LogSanitizer.SanitizeArguments(invocation.Method, invocation.Arguments);

        _logger.LogInformation("[ENTRY] {Class}.{Method}({Arguments})", targetType, methodName, sanitizedArgs);

        var sw = Stopwatch.StartNew();
        var returnType = invocation.Method.ReturnType;

        if (returnType == typeof(Task))
        {
            invocation.Proceed();
            var task = (Task)invocation.ReturnValue;
            invocation.ReturnValue = InterceptTaskAsync(task, targetType, methodName, sw);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            invocation.Proceed();
            var resultType = returnType.GetGenericArguments()[0];
            invocation.ReturnValue = InterceptGenericTaskAsync(invocation.ReturnValue, resultType, targetType, methodName, sw);
        }
        else if (returnType == typeof(ValueTask))
        {
            invocation.Proceed();
            var vTask = (ValueTask)invocation.ReturnValue;
            invocation.ReturnValue = InterceptValueTaskAsync(vTask, targetType, methodName, sw);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            invocation.Proceed();
            var resultType = returnType.GetGenericArguments()[0];
            invocation.ReturnValue = InterceptGenericValueTaskAsync(invocation.ReturnValue, resultType, targetType, methodName, sw);
        }
        else
        {
            // Synchronous method
            try
            {
                invocation.Proceed();
                sw.Stop();
                _logger.LogInformation("[EXIT] {Class}.{Method} (Elapsed: {ElapsedMilliseconds} ms)", targetType, methodName, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "[ERROR] {Class}.{Method} threw exception after {ElapsedMilliseconds} ms: {Message}", targetType, methodName, sw.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private async Task InterceptTaskAsync(Task task, string targetType, string methodName, Stopwatch sw)
    {
        try
        {
            await task.ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("[EXIT] {Class}.{Method} (Elapsed: {ElapsedMilliseconds} ms)", targetType, methodName, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[ERROR] {Class}.{Method} threw exception after {ElapsedMilliseconds} ms: {Message}", targetType, methodName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private object InterceptGenericTaskAsync(object taskObj, Type resultType, string targetType, string methodName, Stopwatch sw)
    {
        var method = TaskInterceptorMethods.GetOrAdd(resultType, t =>
            typeof(ServiceLoggingInterceptor)
                .GetMethod(nameof(InternalInterceptGenericTaskAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(t));

        return method.Invoke(this, new[] { taskObj, targetType, methodName, sw })!;
    }

    private async Task<TResult> InternalInterceptGenericTaskAsync<TResult>(Task<TResult> task, string targetType, string methodName, Stopwatch sw)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("[EXIT] {Class}.{Method} (Elapsed: {ElapsedMilliseconds} ms)", targetType, methodName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[ERROR] {Class}.{Method} threw exception after {ElapsedMilliseconds} ms: {Message}", targetType, methodName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private async ValueTask InterceptValueTaskAsync(ValueTask task, string targetType, string methodName, Stopwatch sw)
    {
        try
        {
            await task.ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("[EXIT] {Class}.{Method} (Elapsed: {ElapsedMilliseconds} ms)", targetType, methodName, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[ERROR] {Class}.{Method} threw exception after {ElapsedMilliseconds} ms: {Message}", targetType, methodName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private object InterceptGenericValueTaskAsync(object taskObj, Type resultType, string targetType, string methodName, Stopwatch sw)
    {
        var method = ValueTaskInterceptorMethods.GetOrAdd(resultType, t =>
            typeof(ServiceLoggingInterceptor)
                .GetMethod(nameof(InternalInterceptGenericValueTaskAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(t));

        return method.Invoke(this, new[] { taskObj, targetType, methodName, sw })!;
    }

    private async ValueTask<TResult> InternalInterceptGenericValueTaskAsync<TResult>(ValueTask<TResult> task, string targetType, string methodName, Stopwatch sw)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("[EXIT] {Class}.{Method} (Elapsed: {ElapsedMilliseconds} ms)", targetType, methodName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[ERROR] {Class}.{Method} threw exception after {ElapsedMilliseconds} ms: {Message}", targetType, methodName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
