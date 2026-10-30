namespace FlashCsv.Annotations;

/// <summary>
/// Valide que la longueur d'une chaîne est supérieure ou égale à une borne minimale.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvMinLengthAttribute(int min) : Attribute
{
    public int Value => min;
}
