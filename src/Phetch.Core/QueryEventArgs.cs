namespace Phetch.Core;

using System;

/// <summary>
/// Object containing information about a succeeded query.
/// </summary>
public sealed class QuerySuccessEventArgs<TArg, TResult> : EventArgs
{
    /// <summary>
    /// The original argument passed to the query.
    /// </summary>
    public TArg Arg { get; }

    /// <summary>
    /// The value returned by the query.
    /// </summary>
    public TResult Result { get; }

    /// <summary>
    /// Creates a new QuerySuccessEventArgs
    /// </summary>
    public QuerySuccessEventArgs(TArg arg, TResult result)
    {
        Arg = arg;
        Result = result;
    }
}

/// <summary>
/// Object containing information about a succeeded query, without type information.
/// </summary>
public abstract class QueryFailureEventArgs : EventArgs
{
    /// <summary>
    /// The exception thrown by the query.
    /// </summary>
    public Exception Exception { get; }

    internal QueryFailureEventArgs(Exception exception)
    {
        Exception = exception;
    }
}

/// <summary>
/// Object containing information about a succeeded query.
/// </summary>
public sealed class QueryFailureEventArgs<TArg> : QueryFailureEventArgs
{
    /// <summary>
    /// The original argument passed to the query.
    /// </summary>
    public TArg Arg { get; }

    /// <summary>
    /// Creates a new QueryFailureEventArgs
    /// </summary>
    public QueryFailureEventArgs(TArg arg, Exception exception) : base(exception)
    {
        Arg = arg;
    }
}
