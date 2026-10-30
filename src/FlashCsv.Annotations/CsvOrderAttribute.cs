namespace FlashCsv.Annotations;

/// <summary>
/// Définit l'ordre d'écriture de cette colonne lors de la sérialisation.
/// Les propriétés avec l'ordre le plus petit sont écrites en premier.
/// </summary>
/// <param name="order">Position de la colonne (les valeurs plus basses apparaissent à gauche).</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvOrderAttribute(int order) : Attribute
{
    public int Order => order;
}
