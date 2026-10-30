namespace FlashCsv.Annotations;

/// <summary>
/// Indique qu'un tableau ou une liste est sérialisé/désérialisé dans une seule cellule CSV 
/// en séparant ses éléments par un délimiteur interne (par défaut ';').
/// </summary>
/// <param name="delimiter">Caractère délimiteur entre éléments (par défaut ';').</param>
/// <param name="escapeChar">Caractère d'échappement (par exemple '\\' ou '\0' si désactivé).</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvDelimiterArrayAttribute(char delimiter = ';', char escapeChar = '\0') : Attribute
{
    public char Delimiter => delimiter;
    public char EscapeChar => escapeChar;
}
