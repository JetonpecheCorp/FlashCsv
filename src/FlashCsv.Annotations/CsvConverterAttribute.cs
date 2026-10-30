namespace FlashCsv.Annotations;

/// <summary>
/// Marqueur générique pour brancher un convertisseur typé sur une colonne sans passer par la réflexion.
/// </summary>
/// <typeparam name="TConverter">Type implémentant <see cref="ICsvFieldReader{T}"/> ou <see cref="ICsvFieldWriter{T}"/>.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CsvConverterAttribute<TConverter>: Attribute where TConverter : ICsvFieldConverter;
