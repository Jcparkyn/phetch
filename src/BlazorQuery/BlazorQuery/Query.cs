﻿namespace BlazorQuery;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

public abstract class QueryBase<TResult>
{
    public TResult? Data { get; protected set; }

    public Exception? Error { get; protected set; }

    public bool IsLoading { get; protected set; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Error is not null;

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess => Data is not null && !IsLoading && !IsError;
}

public class Query<TResult> : QueryBase<TResult>
{
    private readonly Func<Task<TResult>> _action;
    private readonly Action? _onStateChanged;

    public Query(Action? onStateChanged, Func<Task<TResult>> action)
    {
        _action = action;
        _onStateChanged = onStateChanged;
        _ = Refetch();
    }

    public async Task<TResult?> Refetch()
    {
        IsLoading = true;
        _onStateChanged?.Invoke();
        try
        {
            Data = await _action();
            return Data;
        }
        catch (Exception ex)
        {
            Error = ex;
            return default;
        }
        finally
        {
            IsLoading = false;
            _onStateChanged?.Invoke();
        }
    }
}

public class Query<TArg, TResult> : QueryBase<TResult>
{
    private readonly Func<TArg, CancellationToken, Task<TResult>> _action;
    private readonly Action? _onStateChanged;

    private TArg? _lastArg;
    private Task<TResult>? _lastActionCall;
    private CancellationTokenSource _cts = new();

    public Query(Action? onStateChanged, Func<TArg, CancellationToken, Task<TResult>> action)
    {
        _action = action;
        _onStateChanged = onStateChanged;
    }

    public async Task<TResult?> SetParams(TArg arg)
    {
        if (Equals(arg, _lastArg)) // TODO remove boxing
        {
            return Data;
        }
        _lastArg = arg;
        IsLoading = true;
        _onStateChanged?.Invoke();

        if (_lastActionCall is not null && !_lastActionCall.IsCompleted)
        {
            _cts.Cancel();
            _cts = new();
        }

        var thisActionCall = _action(arg, _cts.Token);
        _lastActionCall = thisActionCall;

        TResult? newData;
        try
        {
            newData = await thisActionCall;
            // Only update if no new calls have been started since this one started.
            if (thisActionCall.Id == _lastActionCall.Id)
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
            if (thisActionCall.Id == _lastActionCall.Id)
            {
                Error = ex;
                IsLoading = false;
                _onStateChanged?.Invoke();
            }
            return default;
        }
    }
}
