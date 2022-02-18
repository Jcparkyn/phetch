namespace BlazorQuery;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

public class Query<TResult>
{
    public TResult? Data { get; private set; }

    public Exception? Error { get; private set; }

    public bool IsLoading { get; private set; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Error is not null;

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess => Data is not null && !IsLoading && !IsError;

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

public class Query<TArg, TResult>
{
    public TResult? Data { get; private set; }

    public Exception? Error { get; private set; }

    public bool IsLoading { get; private set; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Error is not null;

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess => Data is not null && !IsLoading && !IsError;

    private readonly Func<TArg, Task<TResult>> _action;
    private readonly Action? _onStateChanged;
    private TArg? _currentArg;

    public Query(Action? onStateChanged, Func<TArg, Task<TResult>> action)
    {
        _action = action;
        _onStateChanged = onStateChanged;
    }

    public async Task<TResult?> SetParams(TArg arg)
    {
        if (Equals(arg, _currentArg))
        {
            return Data;
        }
        _currentArg = arg;
        IsLoading = true;
        _onStateChanged?.Invoke();
        try
        {
            Data = await _action(arg);
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
