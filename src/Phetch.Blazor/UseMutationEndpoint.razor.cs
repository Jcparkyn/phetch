namespace Phetch.Blazor;

using System;
using Microsoft.AspNetCore.Components;
using Phetch.Core;

public sealed partial class UseMutationEndpoint<TArg, TResult> : IDisposable
{
    private Query<TArg, TResult>? _mutation;
    private QueryEndpoint<TArg, TResult>? _endpoint;

    [Parameter, EditorRequired]
    public QueryEndpoint<TArg, TResult>? Endpoint
    {
        get => _endpoint;
        set
        {
            if (ReferenceEquals(_endpoint, value))
                return;
            TryUnsubscribe(_mutation);
            if (value is not null)
            {
                _mutation = GetQuery(value);
                _endpoint = value;
            }
        }
    }

    [Parameter, EditorRequired]
    public RenderFragment<Query<TArg, TResult>> ChildContent { get; set; } = null!;

    [Parameter]
    public Action<TResult>? OnSuccess { get; set; }

    [Parameter]
    public Action<Exception>? OnFailure { get; set; }

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_mutation);
    }

    private Query<TArg, TResult> GetQuery(QueryEndpoint<TArg, TResult> endpoint)
    {
        var newQuery = endpoint.Use();
        newQuery.StateChanged += StateHasChanged;
        // TODO
        //newQuery.Succeeded += SuccessCallback;
        //newQuery.Failed += FailureCallback;
        return newQuery;
    }

    private void TryUnsubscribe(Query<TArg, TResult>? mutation)
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
