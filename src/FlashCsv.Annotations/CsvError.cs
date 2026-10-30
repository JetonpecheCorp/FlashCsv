using System.Text;

namespace FlashCsv.Annotations;

/// <summary>
/// Représente le détail contextuel d'une erreur survenue lors du parsing d'un champ ou d'une ligne, sans allocation sur le tas.
/// </summary>
/// <param name="LineNumber">Numéro de ligne physique ou logique (commençant à 1).</param>
/// <param name="ColumnIndex">Index base zéro de la colonne concernée.</param>
/// <param name="ColumnName">Nom d'en-tête de la colonne si les en-têtes sont activés ; sinon <see langword="null"/>.</param>
/// <param name="Code">Code d'erreur catégorisant l'anomalie.</param>
/// <param name="RawValue">Segment mémoire contenant les octets bruts en UTF-8 ayant déclenché l'erreur.</param>
public readonly record struct CsvError(
    long LineNumber,
    int ColumnIndex,
    string? ColumnName,
    CsvErrorCode Code,
    ReadOnlyMemory<byte> RawValue)
{
    /// <summary>
    /// Restitue la valeur brute sous forme de chaîne de caractères UTF-8 sans manipulation manuelle.
    /// </summary>
    public string RawText => RawValue.IsEmpty ? string.Empty : Encoding.UTF8.GetString(RawValue.Span);

    /// <summary>
    /// Décode la valeur brute en chaîne de caractères UTF-8 pour l'affichage ou la journalisation.
    /// </summary>
    /// <returns>La représentation textuelle de la valeur erronée, ou une chaîne vide si aucune donnée n'est présente.</returns>
    public string GetValueAsString() =>
        RawValue.IsEmpty ? string.Empty : Encoding.UTF8.GetString(RawValue.Span);

    /// <summary>
    /// Fournit un message de diagnostic prêt à l'emploi indiquant la ligne, la colonne, le code et la valeur brute.
    /// </summary>
    public override string ToString()
    {
        string colDesc = string.IsNullOrEmpty(ColumnName)
            ? $"Col {ColumnIndex}" 
            : $"Col {ColumnIndex} ('{ColumnName}')";

        return $"[Ligne {LineNumber}, {colDesc}] Code: {Code} | Valeur: '{RawText}'";
    }
}