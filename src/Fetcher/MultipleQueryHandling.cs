namespace Fetcher;

/// <summary>
/// Controls what happens when a new query is triggered while an existing query is in progress.
/// </summary>
public enum MultipleQueryHandling
{
    /// <summary>
    /// If a new query is triggered while an existing query is in progress, the existing query will be
    /// cancelled using the passed <see cref="System.Threading.CancellationToken"/>.
    /// </summary>
    /// <remarks>
    /// This feature relies upon the CancellationToken being used correctly by the query function,
    /// so it will have no effect is the CancellationToken is ignored.
    /// </remarks>
    CancelRunningQueries,

    /// <summary>
    /// If a new query is triggered while an existing query is in progress, the new query will be
    /// "queued" to start when the previous query finishes. This prevents multiple queries from
    /// being executed at the same time.
    /// </summary>
    /// <remarks>
    /// Any subsequent queries triggered while a query is queued will replace the queued task, so
    /// there is at most one query queued at a time.
    /// </remarks>
    QueueNewest,
}
