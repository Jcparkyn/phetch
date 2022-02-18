namespace BlazorQuery;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

public class Query<TArg, TResult>
{
    private readonly Func<TArg, CancellationToken, Task<TResult>> _action;
    private readonly Action? _onStateChanged;

    private TArg? _lastArg;
    private Task<TResult>? _lastActionCall;
    private CancellationTokenSource _cts = new();

    public TResult? Data { get; protected set; }

    public Exception? Error { get; protected set; }

    public bool IsLoading { get; protected set; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Error is not null;

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess => Data is not null && !IsLoading && !IsError;

    public bool IsUninitialized => _lastActionCall is null;

    public Query(
        Action? onStateChanged,
        Func<TArg, CancellationToken, Task<TResult>> action)
    {
        _action = action;
        _onStateChanged = onStateChanged;
    }

    public async Task<TResult?> Refetch()
    {
        if (IsUninitialized && typeof(TArg) != typeof(Unit))
        {
            return default; // TODO throw?
        }
        return await SetParams(_lastArg!, true);
    }

    public async Task<TResult?> SetParams(TArg arg, bool forceLoad = false)
    {
        if (!forceLoad & !IsUninitialized && EqualityComparer<TArg>.Default.Equals(arg, _lastArg!))
        {
            return Data;
        }

        _lastArg = arg;
        IsLoading = true;
        Error = null;

        _onStateChanged?.Invoke();

        CancelQueriesInProgress();

        var thisActionCall = _action(arg, _cts.Token);
        _lastActionCall = thisActionCall;
        try
        {
            var newData = await thisActionCall;
            // Only update if no new calls have been started since this one started.
            if (thisActionCall == _lastActionCall)
            {
                IsLoading = false;
                Data = newData;
                Error = null;
                _onStateChanged?.Invoke();
            }
            return newData;
        }
        catch (TaskCanceledException)
        {
            return Data;
        }
        catch (Exception ex)
        {
            // Only update if no new calls have been started since this one started.
            if (thisActionCall == _lastActionCall)
            {
                Error = ex;
                IsLoading = false;
                _onStateChanged?.Invoke();
            }
            return default;
        }
    }

    private void CancelQueriesInProgress()
    {
        if (_lastActionCall is not null && !_lastActionCall.IsCompleted)
        {
            _cts.Cancel();
            _cts = new();
        }
    }
}

public class Query<TResult> : Query<Unit, TResult>
{
    public Query(Action? onStateChanged, Func<CancellationToken, Task<TResult>> action)
        : base(onStateChanged, (_, token) => action(token))
    {
        _ = SetParams(default, true); // Trigger an initial query
    }
}
