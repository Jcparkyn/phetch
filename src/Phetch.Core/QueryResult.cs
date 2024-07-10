namespace Phetch.Core;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

internal static class QueryResult
{
    internal static async Task<QueryResult<T>> OfAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return new(await func());
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        {
            return new(ex);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}

/// <summary>
/// Represents the result of a query invokation, which either succeeded with data or failed with an exception.
/// </summary>
/// <remarks>
/// This type mostly exists so that methods like <see
/// cref="Query{TArg,TResult}.TriggerAsync">TriggerAsync</see> don't need to re-throw the exception
/// directly, which makes them safe to use in Blazor component callbacks.
/// </remarks>
/// <typeparam name="TResult"></typeparam>
public record QueryResult<TResult>
{
    /// <summary>
    /// Whether or not the query succeeded (completed without throwing an exception).
    /// </summary>
    // Can't use MemberNotNullWhen for Result because it lies if TResult is nullable
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    /// <summary>
    /// The exception thrown by the query if it failed, otherwise <see langword="null"/>.
    /// </summary>
    ///
    public Exception? Error { get; }

    /// <summary>
    /// The result returned by the query if it succeeded, otherwise <see langword="default"/>.
    /// </summary>
    public TResult? Data { get; }

    /// <summary>
    /// Creates a new successful result.
    /// </summary>
    public QueryResult(TResult? result)
    {
        IsSuccess = true;
        Data = result;
        Error = null;
    }

    /// <summary>
    /// Creates a new unsuccessful result.
    /// </summary>
    public QueryResult(Exception error)
    {
        IsSuccess = false;
        Data = default;
        Error = error;
    }

    /// <summary>
    /// Returns the query result if the query succeeded, otherwise throws the exception thrown by
    /// the query.
    /// </summary>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "<Pending>")]
    public TResult GetOrThrow()
    {
        return IsSuccess ? Data! : throw Error;
    }

    /// <summary>
    /// Returns the query result if the query succeeded, otherwise returns <paramref name="defaultValue"/>.
    /// </summary>
    public TResult GetOrDefault(TResult defaultValue)
    {
        return IsSuccess ? Data! : defaultValue;
    }
}

