namespace FlashCsv.Annotations;

/// <summary>
/// Valide que la longueur d'une chaîne est inférieure ou égale à une borne maximale.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvMaxLengthAttribute(int max) : Attribute
{
    public int Value => max;
}
