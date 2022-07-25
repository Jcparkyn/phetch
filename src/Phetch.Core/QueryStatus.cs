namespace Phetch.Core;

/// <summary>
/// A type representing the possible states of a query.
/// </summary>
public enum QueryStatus
{
    /// <summary>
    /// The query has not yet been started with any parameters.
    /// </summary>
    Idle,
    /// <summary>
    /// The query is fetching, and has not previously succeeded.
    /// </summary>
    Loading,
    /// <summary>
    /// The most recent invokation of the query has failed.
    /// </summary>
    Error,
    /// <summary>
    /// The most recent invokation of the query has succeeded.
    /// </summary>
    Success,
}
