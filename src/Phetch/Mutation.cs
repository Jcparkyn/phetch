namespace Phetch;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

public class Mutation<TArg, TResult>
{
    private readonly Func<TArg, Task<TResult>> _mutationFn;
    private readonly MutationOptions<TResult> _options;
    private readonly MutationEndpointOptions<TResult>? _endpointOptions;

    private Task<TResult>? _lastActionCall;

    public event Action? StateChanged = delegate { };

    public QueryStatus Status { get; private set; } = QueryStatus.Idle;

    public TResult? Data { get; protected set; }

    public Exception? Error { get; protected set; }

    public bool IsLoading => Status == QueryStatus.Loading;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => Error is not null && Status == QueryStatus.Error;

    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccess => Data is not null && Status == QueryStatus.Success;

    public bool IsUninitialized => Status == QueryStatus.Idle;

    public Mutation(
        Func<TArg, Task<TResult>> mutationFn,
        MutationOptions<TResult> options,
        MutationEndpointOptions<TResult>? endpointOptions = null)
    {
        _mutationFn = mutationFn;
        _options = options;
        _endpointOptions = endpointOptions;
    }

    public void Trigger(TArg arg) => _ = TriggerAsync(arg);

    public async Task<TResult> TriggerAsync(TArg arg)
    {
        Status = QueryStatus.Loading;
        Error = null;

        StateChanged?.Invoke();

        var thisActionCall = _mutationFn(arg);
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
                _options?.OnSuccess?.Invoke(newData);
                StateChanged?.Invoke();
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
                _endpointOptions?.OnFailure?.Invoke(ex);
                _options?.OnFailure?.Invoke(ex);
                StateChanged?.Invoke();
            }

            throw;
        }
    }
}

public class Mutation<TArg> : Mutation<TArg, Unit>
{
    public Mutation(
        Func<TArg, Task> mutationFn
    ) : base(
        async arg =>
        {
            await mutationFn(arg);
            return new Unit();
        },
        new())
    {
    }
}
