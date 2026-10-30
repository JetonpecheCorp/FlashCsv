namespace FlashCsv.Annotations;

/// <summary>
/// Indique qu'un champ CSV devient obligatoire si la condition statique donnée est vérifiée.
/// </summary>
/// <typeparam name="TCondition">Type implémentant <see cref="ICsvCondition{T}"/>.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvRequiredIfAttribute<TCondition> : Attribute;
