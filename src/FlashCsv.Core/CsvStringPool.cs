using System.Runtime.CompilerServices;
using System.Text;

namespace FlashCsv.Core;

public sealed class CsvStringPool
{
    public static readonly CsvStringPool Shared = new();

    private const int PoolSize = 2048;
    private const int PoolMask = PoolSize - 1;
    private readonly string?[] _pool = new string?[PoolSize];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetOrAdd(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return string.Empty;

        uint hash = ComputeFastHash(span);
        int index = (int)(hash & PoolMask);
        string? cached = _pool[index];

        // Vérification rapide de taille puis comparaison UTF-8 sans décoder
        if (cached != null && AsciiEquals(span, cached))
        {
            return cached;
        }

        string value = Encoding.UTF8.GetString(span);
        _pool[index] = value;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AsciiEquals(ReadOnlySpan<byte> utf8Bytes, string candidate)
    {
        if (utf8Bytes.Length != candidate.Length) return false;

        // Comparaison caractère par caractère (fast-path ASCII)
        for (int i = 0; i < utf8Bytes.Length; i++)
        {
            if (utf8Bytes[i] != candidate[i]) return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ComputeFastHash(ReadOnlySpan<byte> span)
    {
        uint hash = 2166136261u;
        for (int i = 0; i < span.Length; i++)
        {
            hash = (hash ^ span[i]) * 16777619u;
        }
        return hash;
    }
}