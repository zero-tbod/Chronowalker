namespace Chronowalker.Services;

/// <summary>
/// Resolves localized resources for the user interface.
/// </summary>
internal interface IStringLocalizer
{
    /// <summary>
    /// Gets a localized string by resource key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string.</returns>
    string Get(string key);

    /// <summary>
    /// Formats a localized string with culture-aware arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="arguments">The format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    string Format(string key, params object[] arguments);
}
