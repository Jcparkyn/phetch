namespace Phetch.Core;

using System;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// An interface to allow queries to be retried when they fail.
/// </summary>
public interface IRetryHandler
{
    /// <summary>
    /// Invokes the provided query function and optionally handles errors.
    /// </summary>
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> queryFn, CancellationToken ct);
}

/// <summary>
/// A helper class for creating retry handlers.
/// </summary>
public static class RetryHandler
{
    private static NoRetryHandler? s_none;

    /// <summary>
    /// If passed in the options to <see cref="Endpoint{TArg, TResult}.Use">Endpoint.Use</see>, this will override the default retry
    /// handler for the endpoint.
    /// <para/>
    /// Example:
    /// <code>
    /// var query = endpoint.Use(new()
    /// {
    ///     RetryHandler = RetryHandler.None
    /// });
    /// </code>
    /// </summary>
    public static NoRetryHandler None => s_none ??= new();

    /// <inheritdoc cref="SimpleRetryHandler"/>
    public static SimpleRetryHandler Simple(int maxRetries) => new(maxRetries);
}

/// <summary>
/// A simple retry handler that retries a given number of times with no waiting between retries.
/// Pass the <see cref="ExecuteAsync">ExecuteAsync</see> method to the <see
/// cref="EndpointOptions{TArg,TResult}.RetryHandler"/> property of an <see cref="Endpoint{TArg,
/// TResult}"/> to use this retry handler.
/// </summary>
/// <remarks>
/// Construct this using <see cref="RetryHandler.Simple(int)"/>.
/// <para/>
/// <example><b>Example:</b>
/// <code>
///RetryHandler = RetryHandler.Simple(2);
/// </code>
/// </example>
/// </remarks>
public sealed class SimpleRetryHandler : IRetryHandler
{
    /// <summary>
    /// The maximum number of times to retry before giving up.
    /// </summary>
    public int MaxRetries { get; }

    internal SimpleRetryHandler(int maxRetries)
    {
        MaxRetries = maxRetries;
    }

    /// <inheritdoc/>
    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> queryFn, CancellationToken ct)
    {
        _ = queryFn ?? throw new ArgumentNullException(nameof(queryFn));
        var tryCount = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var result = await queryFn(ct).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var canRetry = tryCount < MaxRetries;
                if (!canRetry)
                {
                    throw;
                }
            }

            tryCount++;
        }
    }
}

/// <summary>
/// A retry handler that can be used to override the retry handler for an endpoint, so that no
/// retries are made.
/// </summary>
public sealed class NoRetryHandler : IRetryHandler
{
    internal NoRetryHandler() { }

    /// <inheritdoc/>
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> queryFn, CancellationToken ct)
    {
        _ = queryFn ?? throw new ArgumentNullException(nameof(queryFn));
        return queryFn(ct);
    }
}

