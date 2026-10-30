namespace FlashCsv.Annotations;

public enum CsvEnumFormat
{
    /// <summary>
    /// Désérialise et sérialise via le nom littéral de l'énumération (ex. "Pending").
    /// </summary>
    Name = 0,

    /// <summary>
    /// Désérialise et sérialise via la valeur entière sous-jacente (ex. 1).
    /// </summary>
    UnderlyingValue = 1
}

/// <summary>
/// Configure le format de sérialisation et désérialisation d'une énumération.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvEnumAttribute(CsvEnumFormat format = CsvEnumFormat.Name) : Attribute
{
    public CsvEnumFormat Format { get; } = format;
}
