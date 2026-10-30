namespace FlashCsv.Annotations;

/// <summary>
/// Contrat statique abstrait pour le parsing zéro-allocation d'un champ UTF-8 vers un type cible.
/// </summary>
/// <typeparam name="T">Type résultant de la conversion.</typeparam>
public interface ICsvFieldReader<T>: ICsvFieldConverter
{
    /// <summary>
    /// Tente de désérialiser une séquence d'octets UTF-8 en une instance de <typeparamref name="T"/>.
    /// </summary>
    /// <param name="source">Octets bruts composant la cellule CSV.</param>
    /// <param name="value">Valeur convertie en cas de succès.</param>
    /// <param name="errorCode">Code d'erreur descriptif si la conversion échoue.</param>
    /// <returns><see langword="true"/> si la conversion réussit ; sinon <see langword="false"/>.</returns>
    static abstract bool TryParse(
        ReadOnlySpan<byte> source,
        out T value,
        out CsvErrorCode errorCode
    );
}
