namespace FlashCsv.Annotations;

/// <summary>
/// Définit le nom ou les alias de la colonne CSV correspondant à cette propriété.
/// En lecture, tous les alias sont testés. En écriture, le premier nom est utilisé.
/// </summary>
/// <param name="names">Liste des noms admissibles (insensible à la casse en lecture).</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvNameAttribute(params string[] names) : Attribute
{
    public string[] Names => names;
}
