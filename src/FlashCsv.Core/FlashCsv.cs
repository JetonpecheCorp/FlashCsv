using System.Runtime.CompilerServices;
using System.Text;
using FlashCsv.Annotations;

namespace FlashCsv.Core;

/// <summary>
/// Point d'entrée principal pour la sérialisation et la désérialisation de flux CSV.
/// Fournit des méthodes asynchrones haute performance avec zéro allocation.
/// </summary>
public static class FlashCsvSerializer
{
    /// <summary>
    /// Lit et désérialise un flux CSV sous la forme d'un flux asynchrone (streaming).
    /// </summary>
    /// <typeparam name="T">Le type du modèle de données (doit implémenter <see cref="ICsvSerializable{T}"/>).</typeparam>
    /// <param name="stream">Le flux de données source contenant le CSV.</param>
    /// <param name="options">Les options de configuration pour la lecture du CSV.</param>
    /// <param name="leaveOpen">Indique si le flux source doit rester ouvert après la lecture (<c>false</c> par défaut).</param>
    /// <param name="cancellationToken">Jeton permettant d'annuler l'opération asynchrone.</param>
    /// <returns>Un flux asynchrone (<see cref="IAsyncEnumerable{T}"/>) permettant de traiter les lignes une par une.</returns>
    public static async IAsyncEnumerable<T> DeserializeAsync<T>(
        Stream stream,
        CsvReaderOptions? options = null,
        bool leaveOpen = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : ICsvSerializable<T>
    {
        options ??= new CsvReaderOptions();
        await using var reader = await CsvStreamReader.CreateAsync(stream, options, leaveOpen, cancellationToken).ConfigureAwait(false);

        int[] activeColMap;

        if (options.HasHeader)
        {
            string[] headers = await reader.ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
            activeColMap = T.CreateColumnMap(headers, options.ColumnMapping);
        }
        else
        {
            activeColMap = T.CreateColumnMap([], options.ColumnMapping);
            for (int i = 0; i < activeColMap.Length; i++)
            {
                activeColMap[i] = i;
            }
        }

        while (true)
        {
            while (reader.TryReadNextRowSync<T>(activeColMap, out bool success, out var item, out var error))
            {
                if (!success)
                {
                    if (error.Code == CsvErrorCode.None) continue;

                    // 1. Transmet systématiquement l'erreur au délégué OnError s'il est configuré
                    options.OnError?.Invoke(error);

                    // 2. Si le mode est CollectAndContinue, on passe à la ligne suivante
                    if (options.ErrorMode == CsvErrorMode.CollectAndContinue)
                        continue;

                    // 3. Sinon (FailFast), on termine immédiatement le flux sans lever d'exception
                    yield break;
                }

                yield return item;
            }

            if (!await reader.ReadMoreDataAsync(cancellationToken).ConfigureAwait(false)) break;
        }
    }

    /// <summary>
    /// Sérialise une collection synchrone d'éléments vers un flux de sortie CSV.
    /// </summary>
    /// <typeparam name="T">Le type du modèle de données.</typeparam>
    /// <param name="stream">Le flux de destination vers lequel écrire les données CSV.</param>
    /// <param name="items">La collection d'éléments à sérialiser.</param>
    /// <param name="options">Les options de configuration pour l'écriture du CSV.</param>
    /// <param name="leaveOpen">Indique si le flux de destination doit rester ouvert après l'écriture.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tâche représentant l'opération asynchrone.</returns>
    public static async ValueTask SerializeAsync<T>(Stream stream, IEnumerable<T> items, CsvWriterOptions? options = null, bool leaveOpen = false, CancellationToken cancellationToken = default) where T : ICsvSerializable<T>
    {
        options ??= new CsvWriterOptions();
        await using var writer = new CsvStreamWriter(stream, options, leaveOpen);

        if (options.WriteHeader) await writer.WriteHeaderAsync(T.GetHeaderNames(), cancellationToken).ConfigureAwait(false);

        foreach (var item in items) await item.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sérialise un flux asynchrone (ex: Entity Framework <see cref="IAsyncEnumerable{T}"/>) vers un flux de sortie CSV sans allouer de mémoire pour la liste complète.
    /// </summary>
    /// <typeparam name="T">Le type du modèle de données.</typeparam>
    /// <param name="stream">Le flux de destination.</param>
    /// <param name="items">La séquence asynchrone d'éléments à sérialiser.</param>
    /// <param name="options">Les options de configuration pour l'écriture du CSV.</param>
    /// <param name="leaveOpen">Indique si le flux de destination doit rester ouvert après l'écriture.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une tâche représentant l'opération asynchrone.</returns>
    public static async ValueTask SerializeAsync<T>(Stream stream, IAsyncEnumerable<T> items, CsvWriterOptions? options = null, bool leaveOpen = false, CancellationToken cancellationToken = default) where T : ICsvSerializable<T>
    {
        options ??= new CsvWriterOptions();
        await using var writer = new CsvStreamWriter(stream, options, leaveOpen);

        if (options.WriteHeader) await writer.WriteHeaderAsync(T.GetHeaderNames(), cancellationToken).ConfigureAwait(false);

        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false)) await item.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sérialise une collection d'éléments sous la forme d'une chaîne de caractères UTF-16 (Asynchrone).
    /// </summary>
    /// <typeparam name="T">Le type du modèle de données.</typeparam>
    /// <param name="items">La collection d'éléments à sérialiser.</param>
    /// <param name="options">Les options de configuration.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le contenu CSV complet sous forme de <see cref="string"/>.</returns>
    public static async Task<string> SerializeToStringAsync<T>(IEnumerable<T> items, CsvWriterOptions? options = null, CancellationToken cancellationToken = default) where T : ICsvSerializable<T>
    {
        await using var ms = new MemoryStream();
        await SerializeAsync(ms, items, options, false, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    /// <summary>
    /// Sérialise une collection d'éléments sous la forme d'un tableau d'octets UTF-8 (Asynchrone).
    /// </summary>
    /// <typeparam name="T">Le type du modèle de données.</typeparam>
    /// <param name="items">La collection d'éléments à sérialiser.</param>
    /// <param name="options">Les options de configuration.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le contenu CSV complet sous forme de tableau d'octets.</returns>
    public static async Task<byte[]> SerializeToUtf8BytesAsync<T>(IEnumerable<T> items, CsvWriterOptions? options = null, CancellationToken cancellationToken = default) where T : ICsvSerializable<T>
    {
        await using var ms = new MemoryStream();
        await SerializeAsync(ms, items, options, false, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }

    /// <summary>
    /// Sérialise une collection d'éléments sous la forme d'une chaîne de caractères UTF-16 (Bloquant).
    /// </summary>
    public static string SerializeToString<T>(IEnumerable<T> items, CsvWriterOptions? options = null) where T : ICsvSerializable<T> => 
        SerializeToStringAsync(items, options).GetAwaiter().GetResult();

    /// <summary>
    /// Sérialise une collection d'éléments sous la forme d'un tableau d'octets UTF-8 (Bloquant).
    /// </summary>
    public static byte[] SerializeToUtf8Bytes<T>(IEnumerable<T> items, CsvWriterOptions? options = null) where T : ICsvSerializable<T> => 
        SerializeToUtf8BytesAsync(items, options).GetAwaiter().GetResult();

    /// <summary>
    /// Désérialise une chaîne CSV directement vers une liste d'éléments (Asynchrone).
    /// </summary>
    /// <typeparam name="T">Le type du modèle de données.</typeparam>
    /// <param name="csvText">Le contenu CSV sous forme de texte.</param>
    /// <param name="options">Les options de configuration.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une liste instanciée d'éléments désérialisés.</returns>
    public static async Task<List<T>> DeserializeAsync<T>(string csvText, CsvReaderOptions? options = null, CancellationToken cancellationToken = default) where T : ICsvSerializable<T>
    {
        if (string.IsNullOrEmpty(csvText)) return [];
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(csvText);
        return await DeserializeAsync<T>(utf8Bytes.AsMemory(), options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Désérialise une séquence mémoire UTF-8 directement vers une liste d'éléments (Asynchrone).
    /// </summary>
    /// <typeparam name="T">Le type du modèle de données.</typeparam>
    /// <param name="utf8Csv">Le contenu CSV en mémoire encodé en UTF-8.</param>
    /// <param name="options">Les options de configuration.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Une liste instanciée d'éléments désérialisés.</returns>
    public static async Task<List<T>> DeserializeAsync<T>(ReadOnlyMemory<byte> utf8Csv, CsvReaderOptions? options = null, CancellationToken cancellationToken = default) where T : ICsvSerializable<T>
    {
        if (utf8Csv.IsEmpty) return [];
        await using var ms = new MemoryStream(utf8Csv.ToArray());
        var list = new List<T>();
        await foreach (var item in DeserializeAsync<T>(ms, options, false, cancellationToken).ConfigureAwait(false)) list.Add(item);
        return list;
    }

    /// <summary>
    /// Désérialise une chaîne CSV directement vers une liste d'éléments (Bloquant).
    /// </summary>
    public static List<T> Deserialize<T>(string csvText, CsvReaderOptions? options = null) where T : ICsvSerializable<T> => 
        DeserializeAsync<T>(csvText, options).GetAwaiter().GetResult();

    /// <summary>
    /// Désérialise une séquence mémoire UTF-8 directement vers une liste d'éléments (Bloquant).
    /// </summary>
    public static List<T> Deserialize<T>(ReadOnlyMemory<byte> utf8Csv, CsvReaderOptions? options = null) where T : ICsvSerializable<T> => 
        DeserializeAsync<T>(utf8Csv, options).GetAwaiter().GetResult();
}