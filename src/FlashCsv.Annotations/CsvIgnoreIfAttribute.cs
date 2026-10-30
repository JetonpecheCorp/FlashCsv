namespace FlashCsv.Annotations;

/// <summary>
/// Indique que la propriété est ignorée conditionnellement selon une règle statique sans allocation.
/// </summary>
/// <typeparam name="TCondition">Type implémentant <see cref="ICsvCondition{T}"/>.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvIgnoreIfAttribute<TCondition> : Attribute;
