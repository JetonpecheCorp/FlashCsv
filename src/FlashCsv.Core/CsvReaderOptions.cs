using FlashCsv.Annotations;

namespace FlashCsv.Core;

/// <summary>
/// Délégué invoqué lorsqu'une ligne rencontre une erreur de parsing.
/// Renvoie true pour ignorer l'erreur et poursuivre la lecture, ou false pour stopper le flux.
/// </summary>
public delegate void CsvErrorHandler(CsvError error);

/// <summary>
/// Options de configuration immuables gouvernant la lecture et le parsing des flux CSV.
/// </summary>
public sealed record CsvReaderOptions
{
    /// <summary>
    /// Liste ordonnée des séparateurs testés en cas d'auto-détection active.
    /// </summary>
    public char? Delimiter { get; init; } = ',';

    /// <summary>
    /// Détecte et ignore automatiquement l'éventuel BOM UTF-8 (0xEF, 0xBB, 0xBF)
    /// au tout début du flux CSV. Valeur par défaut : true.
    /// </summary>
    public bool DetectAndSkipBom { get; init; } = true;

    /// <summary>
    /// Caractère d'échappement / guillemet (ex. '"').
    /// </summary>
    public char[] CandidateDelimiters { get; init; } = [',', ';', '\t', '|'];

    /// <summary>
    /// Caractère d'encadrement des chaînes (guillemet standard RFC 4180).
    /// </summary>
    public byte Quote { get; init; } = (byte)'"';

    /// <summary>
    /// Préfixe indiquant qu'une ligne doit être ignorée en tant que commentaire (ex. '#').
    /// </summary>
    public byte? CommentPrefix { get; init; } = null;

    /// <summary>
    /// Indique si la première ligne non vide contient les noms de colonnes.
    /// </summary>
    public bool HasHeader { get; init; } = true;

    /// <summary>
    /// Supprime les espaces blancs en début et fin de chaque cellule avant parsing.
    /// </summary>
    public bool TrimWhitespace { get; init; } = false;

    /// <summary>
    /// Ignore les sauts de lignes consécutifs ou vides sans lever d'erreur.
    /// </summary>
    public bool IgnoreBlankLines { get; init; } = true;

    /// <summary>
    /// Politique d'arrêt ou de reprise sur anomalie.
    /// </summary>
    public CsvErrorMode ErrorMode { get; init; } = CsvErrorMode.FailFast;

    /// <summary>
    /// Taille en octets des blocs mémoire alloués lors de la lecture asynchrone par pipeline.
    /// </summary>
    public int BufferSize { get; init; } = 64 * 1024;

/// <summary>
    /// Taille maximale admissible en octets pour une cellule avant d'émettre une erreur de sécurité.
    /// Valeur par défaut : 16 Mo (16 * 1024 * 1024).
    /// </summary>
    public int MaxFieldSize { get; init; } = 16 * 1024 * 1024;

    /// <summary>
    /// Formes textuelles admises comme valant <see langword="true"/> par défaut.
    /// </summary>
    public string[] TrueValues { get; init; } = ["true", "1", "yes", "y", "oui"];

    /// <summary>
    /// Formes textuelles admises comme valant <see langword="false"/> par défaut.
    /// </summary>
    public string[] FalseValues { get; init; } = ["false", "0", "no", "n", "non"];

    /// <summary>
    /// Représentations textuelles par défaut associées à une valeur nulle.
    /// </summary>
    public string[] NullValues { get; init; } = ["", "null", "NULL", "\\N", "NA"];

    /// <summary>
    /// Indique si la virgule (',') doit être acceptée et convertie à la volée en point ('.')
    /// pour le parsing des nombres décimaux (float, double, decimal).
    /// Valeur par défaut : true.
    /// </summary>
    public bool AllowCommaDecimalSeparator { get; init; } = true;

    /// <summary>
    /// Fonction de rappel déclenchée en cas d'erreur de parsing d'une ligne.
    /// Si null ou si le callback renvoie false, la lecture s'interrompt.
    /// </summary>
    public CsvErrorHandler? OnError { get; set; }

    /// <summary>
    /// Active la mise en cache des chaînes de caractères (String Pooling) pour réduire drastiquement
    /// l'allocation mémoire sur les colonnes contenant des valeurs répétitives. (Défaut : true)
    /// </summary>
    public bool UseStringPooling { get; set; } = true;

    /// <summary>
    /// Fonction globale optionnelle de transformation appliquée aux chaînes lues.
    /// Signature : (nomColonne, valeurLue) => valeurModifiée.
    /// </summary>
    public Func<string, string, string>? TransformField { get; init; } = null;

    /// <summary>
    /// Format par défaut appliqué aux dates/heures (DateTime, DateOnly, etc.)
    /// lorsqu'aucun format local [CsvFormat] n'est spécifié, ou en repli.
    /// Exemples : "yyyy-MM-dd", "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss".
    /// Si null, le parseur standard (ISO 8601 / InvariantCulture) est utilisé.
    /// </summary>
    public string? DateTimeFormat { get; init; }

    /// <summary>
    /// Indique si les colonnes surnuméraires (au-delà des en-têtes ou des propriétés du modèle)
    /// doivent être ignorées (true) ou déclencher une erreur TooManyColumns (false).
    /// Valeur par défaut : true.
    /// </summary>
    public bool IgnoreExtraColumns { get; init; } = true;

    /// <summary>
    /// Mode d'assainissement HTML appliqué par défaut à toutes les colonnes de type string.
    /// Si null, aucune modification n'est appliquée.
    /// </summary>
    public HtmlSanitizeMode? DefaultHtmlSanitizeMode { get; init; } = null;

    /// <summary>
    /// Correspondance dynamique entre le nom de la propriété C# (ou son alias de base)
    /// et le nom de colonne effectif présent dans le fichier CSV importé.
    /// Exemple : new() { ["CustomerEmail"] = "adresse_mail", ["VatNumber"] = "tva_intra" }
    /// </summary>
    public IReadOnlyDictionary<string, string>? ColumnMapping { get; init; } = null;
}
