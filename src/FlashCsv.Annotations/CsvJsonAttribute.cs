namespace FlashCsv.Annotations;

/// <summary>
/// Indique que la cellule contient une charge utile JSON à désérialiser directement dans le type de la propriété.
/// </summary>
/// <param name="name">Nom d'en-tête de la colonne CSV.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CsvJsonAttribute(string? name = null): Attribute
{
    /// <summary>
    /// Nom physique optionnel de la colonne dans le fichier CSV si différent de la propriété C#.
    /// </summary>
    public string? Name => name;

    /// <summary>
    /// Type du contexte généré (JsonSerializerContext) pour la compatibilité Native AOT et les performances maximales.
    /// </summary>
    public Type? JsonSourceGenContext { get; init; }
}
