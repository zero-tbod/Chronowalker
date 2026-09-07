using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Chronowalker.Services;

internal sealed class WinUiStringLocalizer : IStringLocalizer
{
    private readonly ResourceLoader _resourceLoader = new();

    public string Get(string key)
    {
        return _resourceLoader.GetString(key);
    }

    public string Format(string key, params object[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
    }
}
