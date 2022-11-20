namespace HackerNewsClient.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

public sealed class QueryParam
{
    private string _value;
    private readonly NavigationManager _navManager;
    private readonly string _name;

    public QueryParam(NavigationManager navManager, string name, string defaultValue)
    {
        _navManager = navManager;
        _name = name;
        var uri = navManager.ToAbsoluteUri(navManager.Uri);
        _value = QueryHelpers.ParseQuery(uri.Query).TryGetValue(name, out var val)
            ? val.ToString()
            : defaultValue;
    }

    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            _navManager.NavigateTo(_navManager.GetUriWithQueryParameter(_name, value), replace: true);
        }
    }
}

public sealed class QueryParam<T> where T : IParsable<T>, IFormattable
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
            var stringValue = value.ToString(null, CultureInfo.InvariantCulture);
            _navManager.NavigateTo(_navManager.GetUriWithQueryParameter(_name, stringValue), replace: true);
        }
    }
}
