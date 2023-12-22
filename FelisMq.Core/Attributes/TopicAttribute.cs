namespace FelisMq.Core.Attributes;

/// <summary>
/// Class that represents the Topic to listen
/// </summary>
public sealed class TopicAttribute : Attribute
{
    public string? Value { get; }

    public TopicAttribute(string? value)
    {
        Value = value;
    }
}