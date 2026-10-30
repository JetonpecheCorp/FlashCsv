namespace FlashCsv.Annotations;

/// <summary>
/// Aplatit les propriétés d'un sous-objet ou sous-struct directement dans les colonnes CSV parentes.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvFlattenAttribute : Attribute
{
    /// <summary>
    /// Préfixe ajouté devant les noms de colonnes du sous-objet (ex: "billing_").
    /// </summary>
    public string Prefix { get; init; } = string.Empty;

    public CsvFlattenAttribute() { }

    public CsvFlattenAttribute(string prefix)
    {
        Prefix = prefix;
    }
}
