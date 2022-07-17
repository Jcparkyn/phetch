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
    public Action<QuerySuccessContext<TArg, TResult>>? OnSuccess { get; set; }

    [Parameter]
    public Action<QueryFailureContext<TArg>>? OnFailure { get; set; }

    void IDisposable.Dispose()
    {
        TryUnsubscribe(_mutation);
    }

    private Query<TArg, TResult> GetQuery(QueryEndpoint<TArg, TResult> endpoint)
    {
        var newQuery = endpoint.Use();
        newQuery.StateChanged += StateHasChanged;
        newQuery.Succeeded += SuccessCallback;
        newQuery.Failed += FailureCallback;
        return newQuery;
    }

    private void TryUnsubscribe(Query<TArg, TResult>? mutation)
    {
        if (mutation is not null)
        {
            mutation.StateChanged -= StateHasChanged;
            mutation.Succeeded -= SuccessCallback;
            mutation.Failed -= FailureCallback;
        }
    }

    private void SuccessCallback(QuerySuccessContext<TArg, TResult> context) { OnSuccess?.Invoke(context); }

    private void FailureCallback(QueryFailureContext<TArg> context) { OnFailure?.Invoke(context); }
}
