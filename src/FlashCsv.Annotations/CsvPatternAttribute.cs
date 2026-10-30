namespace FlashCsv.Annotations;

/// <summary>
/// Valide qu'un champ texte respecte l'expression régulière spécifiée.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvPatternAttribute(string pattern) : Attribute
{
    public string Pattern => pattern;
}
