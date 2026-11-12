using System.Runtime.CompilerServices;
using FlashCsv.Annotations;

namespace FlashCsv.Core;

/// <summary>
/// Contrat pour la sérialisation et la désérialisation d'un type sans imposer de contrainte générique sur le modèle.
/// </summary>
public interface ICsvMapper<T>
{
    bool TryDeserialize(CsvRowView row, ReadOnlySpan<int> colMap, out T result, out CsvError error);
    int[] CreateColumnMap(ReadOnlySpan<string> headers, IReadOnlyDictionary<string, string>? dynamicMapping = null);
    string[] GetHeaderNames();
    ValueTask SerializeAsync(CsvStreamWriter writer, T item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Registre global permettant d'associer un type à son mapper AOT.
/// </summary>
public static class CsvMapperRegistry
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Register<T>(ICsvMapper<T> mapper) => CsvMapperCache<T>.Mapper = mapper;
}

/// <summary>
/// Cache typé statique résolvant le mapper sans réflexion au runtime.
/// </summary>
public static class CsvMapperCache<T>
{
    public static ICsvMapper<T>? Mapper;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ICsvMapper<T> GetMapper()
    {
        if (Mapper != null) return Mapper;

        // Force l'exécution du constructeur de module de l'assembly si pas encore exécuté
        RuntimeHelpers.RunModuleConstructor(typeof(T).Assembly.ManifestModule.ModuleHandle);

        if (Mapper != null) return Mapper;

        throw new InvalidOperationException(
            $"Le type '{typeof(T).FullName}' n'est pas enregistré pour la sérialisation CSV. " +
            $"Assurez-vous qu'il est annoté avec [CsvSerializable].");
    }
}
