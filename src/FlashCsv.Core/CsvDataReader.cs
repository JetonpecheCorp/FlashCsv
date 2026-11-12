using System.Data;
using System.Data.Common;
using System.Reflection;

namespace FlashCsv.Core;

/// <summary>
/// Adaptateur interne permettant de streamer directement le résultat du parseur CSV 
/// vers une base de données (ex: SqlBulkCopy) avec zéro allocation de collection.
/// </summary>
internal sealed class CsvDataReader<T> : DbDataReader
{
    private readonly IAsyncEnumerator<T> _enumerator;
    private readonly PropertyInfo[] _properties;
    private readonly Dictionary<string, int> _ordinals;
    private T _current = default!;
    private bool _isClosed;

    public CsvDataReader(IAsyncEnumerable<T> source)
    {
        _enumerator = source.GetAsyncEnumerator();
        // On récupère uniquement les propriétés lisibles
        _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToArray();
        _ordinals = _properties.Select((p, i) => new { p.Name, i })
                               .ToDictionary(x => x.Name, x => x.i, StringComparer.OrdinalIgnoreCase);
    }

    public CsvDataReader(IEnumerable<T> source) : this(ToAsyncEnumerable(source))
    {
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable(IEnumerable<T> source)
    {
        foreach (var item in source)
            yield return item;
    }

    public override int FieldCount => _properties.Length;
    public override bool HasRows => true;
    public override bool IsClosed => _isClosed;
    public override int RecordsAffected => -1;

    public override string GetName(int ordinal) => _properties[ordinal].Name;
    public override int GetOrdinal(string name) => _ordinals.TryGetValue(name, out int idx) ? idx : -1;
    public override Type GetFieldType(int ordinal) => _properties[ordinal].PropertyType;
    public override object GetValue(int ordinal) => _properties[ordinal].GetValue(_current)!;
    public override bool IsDBNull(int ordinal) => GetValue(ordinal) == null;

    // C'est cette méthode que SqlBulkCopy.WriteToServerAsync appellera
    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            _current = _enumerator.Current;
            return true;
        }
        return false;
    }

    public override bool Read() => ReadAsync(CancellationToken.None).GetAwaiter().GetResult();

    public override void Close()
    {
        _isClosed = true;
        var disposeTask = _enumerator.DisposeAsync();

        if (!disposeTask.IsCompleted)
        {
            // Conversion sécurisée en Task pour bloquer le thread
            disposeTask.AsTask().GetAwaiter().GetResult();
        }
        else
        {
            // S'il est déjà terminé, on peut le lire directement sans allocation
            disposeTask.GetAwaiter().GetResult();
        }
    }

    public override async Task CloseAsync()
    {
        _isClosed = true;
        await _enumerator.DisposeAsync().ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
    }

    // SqlBulkCopy utilise GetValue. Les accesseurs typés ne sont pas strictement nécessaires.
    public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
    public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
    public override char GetChar(int ordinal) => (char)GetValue(ordinal);
    public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);
    public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);
    public override double GetDouble(int ordinal) => (double)GetValue(ordinal);
    public override float GetFloat(int ordinal) => (float)GetValue(ordinal);
    public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
    public override short GetInt16(int ordinal) => (short)GetValue(ordinal);
    public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
    public override long GetInt64(int ordinal) => (long)GetValue(ordinal);
    public override string GetString(int ordinal) => (string)GetValue(ordinal);
    public override int GetValues(object[] values) { for (int i = 0; i < FieldCount; i++) values[i] = GetValue(i); return FieldCount; }

    public override bool NextResult() => false;
    public override int Depth => 0;

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        => throw new NotSupportedException();

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        => throw new NotSupportedException();

    public override IEnumerator<DbDataRecord> GetEnumerator()
        => throw new NotSupportedException();

    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
}
