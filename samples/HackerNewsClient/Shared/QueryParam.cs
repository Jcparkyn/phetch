﻿namespace HackerNewsClient.Shared;

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

public sealed class QueryParam<T> where T : IParsable<T>
{
    private T _value;
    private readonly NavigationManager _navManager;
    private readonly string _name;

    public QueryParam(NavigationManager navManager, string name, T defaultValue)
    {
        _navManager = navManager;
        _name = name;
        var uri = navManager.ToAbsoluteUri(navManager.Uri);
        _value = QueryHelpers.ParseQuery(uri.Query).TryGetValue(name, out var val)
            && T.TryParse(val.ToString(), CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : defaultValue;
    }

    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            var stringValue = value switch
            {
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };
            _navManager.NavigateTo(_navManager.GetUriWithQueryParameter(_name, stringValue), replace: true);
        }
    }
}
