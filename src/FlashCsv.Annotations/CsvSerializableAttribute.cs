namespace FlashCsv.Annotations;

/// <summary>
/// Indique au Source Generator de produire le code de lecture/écriture CSV pour ce type.
/// Le type cible doit être `partial`.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class CsvSerializableAttribute: Attribute;
