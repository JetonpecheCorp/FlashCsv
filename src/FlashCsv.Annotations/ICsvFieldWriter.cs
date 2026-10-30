namespace FlashCsv.Annotations;

/// <summary>
/// Contrat statique abstrait pour la sérialisation zéro-allocation d'une valeur vers un buffer UTF-8.
/// </summary>
/// <typeparam name="T">Type de la valeur à formater.</typeparam>
public interface ICsvFieldWriter<T>: ICsvFieldConverter
{
    /// <summary>
    /// Tente de formater l'instance donnée directement dans le buffer de destination.
    /// </summary>
    /// <param name="value">Donnée source à écrire.</param>
    /// <param name="destination">Tampon mémoire UTF-8 de destination.</param>
    /// <param name="bytesWritten">Nombre d'octets inscrits dans le tampon.</param>
    /// <param name="errorCode">Code d'erreur descriptif en cas d'échec de formatage.</param>
    /// <returns><see langword="true"/> si l'écriture a réussi ; sinon <see langword="false"/>.</returns>
    static abstract bool TryFormat(
        T value,
        Span<byte> destination,
        out int bytesWritten,
        out CsvErrorCode errorCode
    );
}
