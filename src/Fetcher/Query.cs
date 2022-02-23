namespace Fetcher;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Encapsulates an asynchronous query taking one parameter of type <typeparamref name="TArg"/> and
/// returning a result of type <typeparamref name="TResult"/>
/// </summary>
/// <remarks>
/// <para>For queries with no parameters, you can use the <see cref="Query{TResult}"/> class.</para>
/// <para>For queries with multiple parameters, you can use a tuple in place of <c>TArg</c>:
/// <code>Query&lt;(int, string), string&gt;</code>
/// </para>
/// </remarks>
public class Query<TArg, TResult>
{
    private readonly Func<TArg, CancellationToken, Task<TResult>> _queryFn;
    private readonly MultipleQueryHandling _multipleQueryHandling;
    private readonly Action<TResult?>? _onSuccess;
    private readonly Action? _onError;

    private TArg? _lastArg;
    private Task<TResult>? _lastActionCall;
    private CancellationTokenSource _cts = new(); // TODO: Only allocate when needed
    private bool _isQueryQueued;

    public event Action? OnStateChanged;

    public QueryStatus Status { get; private set; } = QueryStatus.Idle;

    public TResult? Data { get; private set; }

    public Exception? Error { get; private set; }

    public bool IsLoading => Status == QueryStatus.Loading;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Error is not null && Status == QueryStatus.Error;

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess => Data is not null && Status == QueryStatus.Success;

    public bool IsUninitialized => Status == QueryStatus.Idle;

    public bool IsFetching => IsLoading || (_lastActionCall is not null && !_lastActionCall.IsCompleted);

    public Query(
        Func<TArg, CancellationToken, Task<TResult>> queryFn,
        Action? onStateChanged,
        TResult? initialData = default,
        MultipleQueryHandling multipleQueryHandling = MultipleQueryHandling.CancelRunningQueries,
        Action<TResult?>? onSuccess = null,
        Action? onError = null)
    {
        OnStateChanged = onStateChanged;
        _queryFn = queryFn;
        Data = initialData;
        _multipleQueryHandling = multipleQueryHandling;
        _onSuccess = onSuccess;
        _onError = onError;
    }

    /// <summary>
    /// Re-runs the query using the same parameters from the last call to <see cref="SetParams"/> or
    /// <see cref="SetParamsAsync"/>.
    /// </summary>
    public void Refetch() => _ = RefetchAsync();

    /// <summary>
    /// Identical to <see cref="Refetch"/>, but returns the result from the query function (or
    /// throws if an error occurred).
    /// </summary>
    /// <remarks>Only use this if you need to use the query result, otherwise use <see cref="Refetch"/>.
    /// </remarks>
    public Task<TResult?> RefetchAsync()
    {
        if (IsUninitialized && typeof(TArg) != typeof(Unit))
        {
            return Task.FromResult(default(TResult)); // TODO throw?
        }
        return SetParamsAsync(_lastArg!, true);
    }

    /// <summary>
    /// Sets the input parameters to the query function, and re-runs the query if the parameters
    /// have changed.
    /// </summary>
    /// <remarks>
    /// It is safe to call this multiple times with the same value (e.g., in the render method of a component).
    /// </remarks>
    /// <param name="arg">The argument to supply to the query function.</param>
    /// <param name="forceLoad">
    /// If true, the query will always be re-run, regardless of whether the parameters changed.
    /// </param>
    public void SetParams(TArg arg, bool forceLoad = false) => _ = SetParamsAsync(arg, forceLoad);

    public async Task<TResult?> SetParamsAsync(TArg arg, bool forceLoad = false)
    {
        if (!forceLoad & !IsUninitialized && EqualityComparer<TArg>.Default.Equals(arg, _lastArg!))
        {
            return Data;
        }

        _lastArg = arg;

        if (IsFetching)
        {
            switch (_multipleQueryHandling)
            {
                case MultipleQueryHandling.QueueNewest:
                    _isQueryQueued = true;
                    return default; // TODO: return next Task instead
                case MultipleQueryHandling.CancelRunningQueries:
                    CancelQueriesInProgress();
                    break;
            }
        }

        if (Status != QueryStatus.Success)
        {
            Status = QueryStatus.Loading;
        }

        OnStateChanged?.Invoke(); // TODO: Avoid unnecessary re-renders

        Task<TResult>? thisActionCall = null;
        try
        {
            thisActionCall = _queryFn(arg, _cts.Token);
            _lastActionCall = thisActionCall;
            var newData = await thisActionCall;
            // Only update if no new calls have been started since this one started.
            if (thisActionCall == _lastActionCall)
            {
                SetSuccessState(newData);
            }
            if (_isQueryQueued)
            {
                _isQueryQueued = false;
                _ = SetParamsAsync(_lastArg!, true);
            }
            return newData;
        }
        catch (Exception ex)
        {
            // Only update if no new calls have been started since this one started.
            if (thisActionCall == _lastActionCall)
            {
                Error = ex;
                Status = QueryStatus.Error;
                _onError?.Invoke();
                OnStateChanged?.Invoke();
            }

            throw;
        }
    }

    private void SetSuccessState(TResult? newData)
    {
        Status = QueryStatus.Success;
        Data = newData;
        Error = null;
        _onSuccess?.Invoke(newData);
        OnStateChanged?.Invoke();
    }

    private void CancelQueriesInProgress()
    {
        _cts.Cancel();
        _cts = new();
    }
}

/// <summary>
/// Encapsulates an asynchronous query taking no parameters and returning a result of type
/// <typeparamref name="TResult"/>
/// </summary>
public class Query<TResult> : Query<Unit, TResult>
{
    public Query(
        Func<CancellationToken, Task<TResult>> queryFn,
        Action? onStateChanged,
        TResult? initialData = default,
        MultipleQueryHandling multipleQueryHandling = MultipleQueryHandling.CancelRunningQueries,
        Action<TResult?>? onSuccess = null,
        Action? onError = null,
        bool runAutomatically = true
    ) : base(
        (_, token) => queryFn(token),
        onStateChanged: onStateChanged,
        initialData: initialData,
        multipleQueryHandling: multipleQueryHandling,
        onSuccess: onSuccess,
        onError: onError)
    {
        if (runAutomatically)
        {
            SetParams(default, true); // Trigger an initial query
        }
    }
}
