namespace FlashCsv.Annotations;

/// <summary>
/// Précise les chaînes représentant une absence de valeur (null) pour cette colonne.
/// </summary>
/// <param name="nullValues">Valeurs candidates (ex. "NA", "NULL", "\N").</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CsvNullValuesAttribute(params string[] nullValues): Attribute
{
    public string[] NullValues => nullValues;
}
