namespace Chronowalker.Core.Models;

/// <summary>
/// Describes the result of a configuration operation.
/// </summary>
public sealed class OperationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult"/> class.
    /// </summary>
    /// <param name="changed">Whether the operation changed local data.</param>
    /// <param name="configPath">The relevant configuration path.</param>
    public OperationResult(bool changed, string configPath)
    {
        Changed = changed;
        ConfigPath = configPath;
    }

    /// <summary>Gets a value indicating whether local data changed.</summary>
    public bool Changed { get; }

    /// <summary>Gets the relevant configuration path.</summary>
    public string ConfigPath { get; }
}
