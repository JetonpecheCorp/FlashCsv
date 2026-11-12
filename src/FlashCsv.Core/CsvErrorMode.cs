namespace FlashCsv.Core;

/// <summary>
/// Détermine le comportement du lecteur face à une erreur de parsing.
/// </summary>
public enum CsvErrorMode : byte
{
    /// <summary>
    /// Interrompt le traitement immédiatement dès la première cellule ou ligne invalide.
    /// </summary>
    FailFast,

    /// <summary>
    /// Journalise le diagnostic dans un collecteur et poursuit la lecture des lignes suivantes.
    /// </summary>
    CollectAndContinue
}
