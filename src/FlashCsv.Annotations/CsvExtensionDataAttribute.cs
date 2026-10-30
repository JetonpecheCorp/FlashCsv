namespace FlashCsv.Annotations;

/// <summary>
/// Capture toutes les colonnes du CSV qui ne correspondent à aucune propriété du modèle 
/// dans un dictionnaire.
/// Doit être appliqué sur une propriété de type Dictionary&lt;string, string&gt;.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvExtensionDataAttribute : Attribute;
