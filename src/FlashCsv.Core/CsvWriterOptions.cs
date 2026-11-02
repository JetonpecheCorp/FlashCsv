namespace FlashCsv.Core;

/// <summary>
/// Options de configuration immuables gouvernant l'écriture et la sérialisation des flux CSV.
/// </summary>
public sealed record CsvWriterOptions
{
    /// <summary>
    /// Séparateur de colonnes utilisé lors de l'écriture.
    /// </summary>
    public byte Delimiter { get; init; } = (byte)',';

    /// <summary>
    /// Caractère d'encadrement pour les champs nécessitant un échappement.
    /// </summary>
    public byte Quote { get; init; } = (byte)'"';

    /// <summary>
    /// Séquence d'octets marquant la fin de ligne (défaut CRLF "\r\n").
    /// </summary>
    public byte[] NewLine { get; set; } = [(byte)'\r', (byte)'\n'];

    /// <summary>
    /// Stratégie d'encadrement par guillemets des cellules exportées.
    /// </summary>
    public QuoteStrategy QuoteStrategy { get; init; } = QuoteStrategy.Minimal;

    /// <summary>
    /// Écrit systématiquement le libellé des colonnes sur la première ligne du fichier produit.
    /// </summary>
    public bool WriteHeader { get; init; } = true;

    /// <summary>
    /// Texte de substitution inséré lorsqu'une valeur nulle est rencontrée.
    /// </summary>
    public string NullFormat { get; init; } = string.Empty;

    /// <summary>
    /// Taille en octets du tampon d'écriture pour l'accumulateur mémoire.
    /// </summary>
    public int BufferSize { get; init; } = 64 * 1024;

    /// <summary>
    /// Neutralise les cellules de texte débutant par =, +, -, @ ou tabulation en les préfixant d'une apostrophe (').
    /// Protège contre l'injection de formules sous Microsoft Excel et LibreOffice.
    /// </summary>
    public bool EscapeFormulas { get; init; } = false;

    /// <summary>
    /// Préfixe le fichier CSV par les 3 octets du BOM UTF-8 (0xEF, 0xBB, 0xBF).
    /// Fortement recommandé pour l'ouverture directe dans Microsoft Excel.
    /// Valeur par défaut : false.
    /// </summary>
    public bool WriteBom { get; init; } = false;

    /// <summary>
    /// Fonction globale optionnelle de transformation appliquée aux chaînes avant écriture.
    /// Signature : (nomColonne, valeurSource) => valeurModifiée.
    /// </summary>
    public Func<string, string, string>? TransformField { get; init; } = null;

    /// <summary>
    /// Fonction globale optionnelle permettant de traduire ou formater dynamiquement
    /// les noms d'en-têtes lors de la sérialisation (ex: internationalisation).
    /// Signature : (nomOriginal) => nomTraduit.
    /// </summary>
    public Func<string, string>? FormatHeader { get; init; } = null;
}
