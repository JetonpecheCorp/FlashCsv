namespace FlashCsv.Annotations;

/// <summary>
/// Neutralise les formules potentielles pour Excel/Calc en préfixant le contenu texte d'une apostrophe si nécessaire.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvSanitizeFormulaAttribute : Attribute;
