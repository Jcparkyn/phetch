namespace Phetch.Core;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

public class Mutation<TArg, TResult>
{
    private readonly Func<TArg, CancellationToken, Task<TResult>> _mutationFn;
    private readonly MutationEndpointOptions<TResult>? _endpointOptions;

    private Task<TResult>? _lastActionCall;
    private CancellationTokenSource _cts = new();

    public event Action? StateChanged = delegate { };
    public event Action<TResult>? Succeeded = delegate { };
    public event Action<Exception>? Failed = delegate { };

    public QueryStatus Status { get; private set; } = QueryStatus.Idle;

    public TResult? Data { get; protected set; }

    public Exception? Error { get; protected set; }

    public bool IsLoading => Status == QueryStatus.Loading;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Status == QueryStatus.Error;

    /// <summary>
    /// True if the mutation has succeeded.
    /// </summary>
    /// <remarks>
    /// In many cases you should prefer to use <see cref="HasData"/> as it works better with
    /// nullable reference types.
    /// </remarks>
    public bool IsSuccess => Status == QueryStatus.Success;

    /// <summary>
    /// True if the mutation has succeeded and returned a non-null response.
    /// </summary>
    /// <remarks>
    /// This is particularly useful in combination with nullable reference types, as it lets you
    /// safely access <see cref="Data"/> without a compiler warning.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(Data))]
    public bool HasData => IsSuccess && Data is not null;

    public bool IsUninitialized => Status == QueryStatus.Idle;

    public Mutation(
        Func<TArg, CancellationToken, Task<TResult>> mutationFn,
        MutationEndpointOptions<TResult>? endpointOptions = null)
    {
        _mutationFn = mutationFn;
        _endpointOptions = endpointOptions;
    }

    public void Cancel()
    {
        if (_lastActionCall is not null && !_lastActionCall.IsCompleted)
        {
            _cts.Cancel();
            _cts = new();
        }
    }

    public void Trigger(TArg arg) => _ = TriggerAsync(arg);

    public async Task<TResult> TriggerAsync(TArg arg)
    {
        Status = QueryStatus.Loading;
        Error = null;

        StateChanged?.Invoke();

        var thisActionCall = _mutationFn(arg, _cts.Token);
        _lastActionCall = thisActionCall;
        try
        {
            var newData = await thisActionCall;
            // Only update if no new calls have been started since this one started.
            if (thisActionCall == _lastActionCall)
            {
                Status = QueryStatus.Success;
                Data = newData;
                Error = null;
                _endpointOptions?.OnSuccess?.Invoke(newData);
                Succeeded?.Invoke(newData);
                StateChanged?.Invoke();
            }
            return newData;
        }
        catch (TaskCanceledException)
        {
            if (Status == QueryStatus.Loading)
            {
                Status = QueryStatus.Idle;
            }
            StateChanged?.Invoke();
            throw;
        }
        catch (Exception ex)
        {
            // Only update if no new calls have been started since this one started.
            if (thisActionCall == _lastActionCall)
            {
                Error = ex;
                Status = QueryStatus.Error;
                _endpointOptions?.OnFailure?.Invoke(ex);
                Failed?.Invoke(ex);
                StateChanged?.Invoke();
            }

            throw;
        }
    }
}

public class Mutation<TArg> : Mutation<TArg, Unit>
{
    public Mutation(
        Func<TArg, CancellationToken, Task> mutationFn,
        MutationEndpointOptions<Unit>? endpointOptions = null
    ) : base(
        async (arg, token) =>
        {
            await mutationFn(arg, token);
            return new Unit();
        },
        endpointOptions)
    {
    }
}
