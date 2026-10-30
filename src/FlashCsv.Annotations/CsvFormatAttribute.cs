namespace FlashCsv.Annotations;

/// <summary>
/// Spécifie le format de date, d'heure ou numérique utilisé pour le parsing et l'écriture.
/// Exemple : [CsvFormat("dd/MM/yyyy")]
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvFormatAttribute(string format) : Attribute
{
    public string Format { get; } = format;
}
