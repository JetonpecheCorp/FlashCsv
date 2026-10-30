namespace FlashCsv.Annotations;

/// <summary>
/// Indique que la propriété doit être ignorée par le parseur lors de la lecture et de l'écriture.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvIgnoreAttribute : Attribute;
