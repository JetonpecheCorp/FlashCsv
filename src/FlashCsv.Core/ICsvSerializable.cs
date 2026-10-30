using FlashCsv.Annotations;

namespace FlashCsv.Core;

/// <summary>
/// Contrat implémenté automatiquement par le Source Generator sur les types marqués [CsvSerializable].
/// </summary>
public interface ICsvSerializable<TSelf> where TSelf : ICsvSerializable<TSelf>
{
    static abstract bool TryDeserialize(CsvRowView row, ReadOnlySpan<int> colMap, out TSelf result, out CsvError error);
    static abstract int[] CreateColumnMap(ReadOnlySpan<string> headers, IReadOnlyDictionary<string, string>? dynamicMapping = null);
    static abstract string[] GetHeaderNames();
    ValueTask SerializeAsync(CsvStreamWriter writer, CancellationToken cancellationToken = default);
}
