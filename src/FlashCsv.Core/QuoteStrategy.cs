namespace FlashCsv.Core;

/// <summary>
/// Stratégie d'encadrement des valeurs par des guillemets lors de l'écriture.
/// </summary>
public enum QuoteStrategy : byte
{
    /// <summary>
    /// N'entoure de guillemets que si le champ contient un séparateur, un saut de ligne ou un guillemet.
    /// </summary>
    Minimal,

    /// <summary>
    /// Encadre systématiquement toutes les cellules de guillemets.
    /// </summary>
    Always,

    /// <summary>
    /// N'applique jamais de guillemets et échoue si un caractère spécial est détecté.
    /// </summary>
    Never
}
