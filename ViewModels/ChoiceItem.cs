namespace Chronowalker.ViewModels;

internal sealed class ChoiceItem<T>
{
    public ChoiceItem(string displayName, T value)
    {
        DisplayName = displayName;
        Value = value;
    }

    public string DisplayName { get; }

    public T Value { get; }
}
