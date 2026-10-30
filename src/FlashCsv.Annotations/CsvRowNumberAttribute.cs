namespace FlashCsv.Annotations;

/// <summary>
/// Injecte automatiquement le numéro de la ligne courante du fichier CSV dans cette propriété.
/// Doit être appliqué sur un type numérique (int, long).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvRowNumberAttribute : Attribute;
