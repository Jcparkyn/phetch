namespace Phetch.Blazor;

using System;
using Microsoft.AspNetCore.Components;
using Phetch.Core;

public sealed partial class UseMutationEndpoint<TArg, TResult> : IDisposable
{
    private Mutation<TArg, TResult>? _mutation;
    private MutationEndpoint<TArg, TResult>? _endpoint;

    [Parameter, EditorRequired]
    public MutationEndpoint<TArg, TResult>? Endpoint
    {
        get => _endpoint;
        set
        {
            if (ReferenceEquals(_endpoint, value))
                return;
            TryUnsubscribe(_mutation);
            if (value is not null)
            {
                _mutation = GetMutation(value);
                _endpoint = value;
            }
        }
    }

    [Parameter, EditorRequired]
    public RenderFragment<Mutation<TArg, TResult>> ChildContent { get; set; } = null!;

    [Parameter]
    public Action<TResult>? OnSuccess { get; set; }

    [Parameter]
    public Action<Exception>? OnFailure { get; set; }

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_mutation);
    }

    private Mutation<TArg, TResult> GetMutation(MutationEndpoint<TArg, TResult> endpoint)
    {
        var newMutation = endpoint.Use();
        newMutation.StateChanged += StateHasChanged;
        // TODO
        //newMutation.Succeeded += SuccessCallback;
        //newMutation.Failed += FailureCallback;
        return newMutation;
    }

    private void TryUnsubscribe(Mutation<TArg, TResult>? mutation)
    {
        if (mutation is not null)
        {
            mutation.StateChanged -= StateHasChanged;
            // TODO
            //mutation.Succeeded -= SuccessCallback;
            //mutation.Failed -= FailureCallback;
        }
    }

    private void SuccessCallback(TResult result) { OnSuccess?.Invoke(result); }

    private void FailureCallback(Exception ex) { OnFailure?.Invoke(ex); }
}
