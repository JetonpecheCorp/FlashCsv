namespace FlashCsv.Annotations;

/// <summary>
/// Valide que la longueur d'une chaîne ou d'une séquence de caractères est comprise entre des bornes minimale et maximale incluses.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvLengthAttribute(int min, int max) : Attribute
{
    public int Min => min;
    public int Max => max;
}
