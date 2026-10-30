namespace FlashCsv.Annotations;

/// <summary>
/// Indique que cette propriété est obligatoire.
/// Si la colonne est absente ou vide lors de la lecture, une erreur <see cref="CsvErrorCode.RequiredFieldMissing"/> sera levée.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvRequiredAttribute : Attribute;
