namespace FlashCsv.Annotations;

/// <summary>
/// Restreint les valeurs admises pour un champ à un ensemble de valeurs autorisées.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvAllowedValuesAttribute(params string[] values) : Attribute
{
    public string[] Values { get; } = values;
}
