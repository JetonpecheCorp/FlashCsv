namespace FlashCsv.Annotations;

/// <summary>
/// Définit l'ensemble des codes de diagnostic émis en cas d'erreur de parsing ou de validation, sans lever d'exception.
/// </summary>
public enum CsvErrorCode : byte
{
    /// <summary>Aucune erreur.</summary>
    None = 0,

    /// <summary>Impossible de convertir la valeur en nombre entier.</summary>
    InvalidInteger,

    /// <summary>
    /// La valeur ne peut pas être convertie dans les types float ou double
    /// </summary>
    InvalidFloat,

    /// <summary>
    /// La valeur ne peut pas être convertie dans le type décimal
    /// </summary>
    InvalidDecimal,

    /// <summary>
    /// La valeur ne correspond à aucun des littéraux booléens autorisés.
    /// </summary>
    InvalidBoolean,

    /// <summary>
    /// La valeur n'est pas une date ou heure reconnue selon le format spécifié.
    /// </summary>
    InvalidDate,

    /// <summary>
    /// La valeur n'est pas une date time reconnue selon le format spécifié.
    /// </summary>
    InvalidDateTime,

    /// <summary>
    /// La valeur ne correspond pas à un identifiant unique (GUID) valide.
    /// </summary>
    InvalidGuid,

    /// <summary>
    /// Le contenu JSON est malformé, tronqué ou incompatible avec le type cible.
    /// </summary>
    InvalidJson,

    /// <summary>
    /// Un champ non-nullable ou marqué comme obligatoire est vide ou absent.
    /// </summary>
    RequiredFieldMissing,

    /// <summary>
    /// Un guillemet ouvrant n'a pas trouvé de guillemet fermant correspondant avant la fin de la ligne ou du fichier.
    /// </summary>
    UnterminatedQuote,

    /// <summary>
    /// La ligne contient moins de colonnes que ce que prévoit le schéma sans que cela soit explicitement autorisé.
    /// </summary>
    RowTooShort,

    /// <summary>
    /// La ligne contient plus de colonnes que le schéma attendu.
    /// </summary>
    RowTooLong,

    /// <summary>
    /// L'heuristique d'analyse automatique n'a pas pu identifier un délimiteur cohérent parmi les candidats.
    /// </summary>
    CannotDetectDelimiter,

    /// <summary>
    /// La structure de la cellule ou de la ligne enfreint les règles de la RFC 4180 (ex. guillemet parasite non échappé).
    /// </summary>
    InvalidFormat,

    /// <summary>
    /// La ligne contient une valeur d'enum qui n'existe pas
    /// </summary>
    InvalidEnum,

    /// <summary>La valeur est en dehors de la plage autorisée (min/max).</summary>
    ValueOutOfRange,

    /// <summary>La chaîne de caractères ne respecte pas la longueur imposée.</summary>
    InvalidLength,

    /// <summary>La valeur ne correspond pas à l'expression régulière (Pattern) exigée.</summary>
    PatternMismatch,

    /// <summary>La ligne contient plus de colonnes que ce qui est défini dans le modèle strict.</summary>
    TooManyColumns,

    /// <summary>La valeur textuelle est trop grande et dépasse la limite de sécurité du buffer.</summary>
    FieldTooLarge,

    /// <summary>La valeur ne respecte pas le format attendu pour ce type de données.</summary>
    InvalidValue
}
