namespace Phetch.Core;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

public static class QueryResult
{
    public static QueryResult<T> OfSuccess<T>(T result) => new(true, null, result);
    public static QueryResult<T> OfFailure<T>(Exception error) => new(false, error, default);
    internal static async Task<QueryResult<T>> OfAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return new(true, null, await func());
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        {
            return new(false, ex, default);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}

public record QueryResult<TResult>
{
    /// <summary>
    /// Whether or not the query succeeded (completed without throwing an exception).
    /// </summary>
    public bool IsSuccess { get; internal init; }

    /// <summary>
    /// The exception thrown by the query if it failed, otherwise <see langword="null"/>.
    /// </summary>
    ///
    [MemberNotNullWhen(false, nameof(IsSuccess))]
    public Exception? Error { get; internal init; }

    /// <summary>
    /// The result returned by the query if it succeeded, otherwise <see langword="default"/>.
    /// </summary>
    public TResult? Result { get; internal init; }

    internal QueryResult(bool isSuccess, Exception? error, TResult? result)
    {
        IsSuccess = isSuccess;
        Error = error;
        Result = result;
    }

    /// <summary>
    /// Returns the query result if the query succeeded, otherwise throws the exception thrown by
    /// the query.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "<Pending>")]
    public TResult GetOrThrow()
    {
        if (IsSuccess)
        {
            return Result!;
        }
        else
        {
            throw Error!;
        }
    }
}

