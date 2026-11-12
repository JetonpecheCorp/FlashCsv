using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FlashCsv.Annotations;
using System.Globalization;
using System.Text;

namespace FlashCsv.Core;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public readonly ref partial struct CsvRowView
{
    private readonly ReadOnlySpan<byte> _rawLine;
    private readonly ReadOnlySpan<(int Offset, int Length)> _fields;

    public CsvReaderOptions Options { get; }
    public string[]? Headers { get; }

    public long LineNumber { get; }
    public int FieldCount => _fields.Length;

    public CsvRowView(ReadOnlySpan<byte> rawLine, ReadOnlySpan<(int Offset, int Length)> fields, long lineNumber, CsvReaderOptions options, string[]? headers = null)
    {
        _rawLine = rawLine;
        _fields = fields;
        LineNumber = lineNumber;
        Options = options;
        Headers = headers;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetRawField(int index)
    {
        if ((uint)index >= (uint)_fields.Length) return [];

        ref readonly var field = ref _fields[index];
        if (field.Length <= 0) return [];

        // Pas de TrimSpan si non activé (déjà vérifié)
        ReadOnlySpan<byte> slice = _rawLine.Slice(field.Offset, field.Length);

        if (Options.TrimWhitespace)
            slice = TrimSpan(slice);

        // Détection rapide de quote sans double vérification
        if (slice.Length >= 2 && slice[0] == Options.Quote && slice[^1] == Options.Quote)
        {
            slice = slice.Slice(1, slice.Length - 2);
        }

        return slice;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNull(int index)
    {
        ReadOnlySpan<byte> field = GetRawField(index);
        if (field.IsEmpty) return true;

        return IsDefaultNullValue(field);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNull(int index, ReadOnlySpan<string> customNullValues)
    {
        ReadOnlySpan<byte> field = GetRawField(index);
        if (field.IsEmpty) return true;

        // Fast-path ASCII ultra-rapide (les null values standard font entre 1 et 4 octets)
        if (customNullValues.IsEmpty)
        {
            return IsDefaultNullValue(field);
        }

        foreach (var candidate in customNullValues)
        {
            if (field.Length == candidate.Length && EqualsIgnoreCaseUtf8(field, candidate))
                return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDefaultNullValue(ReadOnlySpan<byte> field)
    {
        // Options.NullValues par défaut : ["", "null", "NULL", "\\N", "NA"]
        return field.Length switch
        {
            2 => (field[0] == '\\' && (field[1] == 'N' || field[1] == 'n')) ||
                ((field[0] == 'N' || field[0] == 'n') && (field[1] == 'A' || field[1] == 'a')),
            4 => (field[0] == 'N' || field[0] == 'n') &&
                (field[1] == 'U' || field[1] == 'u') &&
                (field[2] == 'L' || field[2] == 'l') &&
                (field[3] == 'L' || field[3] == 'l'),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetInt32(int index, string? columnName, out int value, out CsvError error)
    {
        ReadOnlySpan<byte> field = GetRawField(index);

        if (field.Length > 0 && field.Length <= 11)
        {
            int val = 0;
            bool isValid = true;
            bool isNegative = field[0] == (byte)'-';
            int startIdx = isNegative ? 1 : 0;

            if (startIdx < field.Length)
            {
                for (int i = startIdx; i < field.Length; i++)
                {
                    byte c = field[i];
                    if (c >= (byte)'0' && c <= (byte)'9')
                    {
                        val = (val * 10) + (c - (byte)'0');
                    }
                    else
                    {
                        isValid = false; 
                        break;
                    }
                }
            }
            else
            {
                isValid = false;
            }


            if (isValid)
            {
                value = isNegative ? -val : val;
                error = default;
                return true;
            }
        }

        if (Utf8Parser.TryParse(field, out value, out int bytesConsumed) && bytesConsumed == field.Length)
        {
            error = default;
            return true;
        }

        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidInteger, field.ToArray());
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetInt64(int index, string? columnName, out long value, out CsvError error)
    {
        ReadOnlySpan<byte> field = GetRawField(index);

        if (field.Length > 0 && field.Length <= 20)
        {
            long val = 0;
            bool isValid = true;
            bool isNegative = field[0] == (byte)'-';
            int startIdx = isNegative ? 1 : 0;

            if (startIdx < field.Length)
            {
                for (int i = startIdx; i < field.Length; i++)
                {
                    byte c = field[i];
                    if (c >= (byte)'0' && c <= (byte)'9') val = (val * 10) + (c - (byte)'0');
                    else { isValid = false; break; }
                }
            }
            else isValid = false;

            if (isValid)
            {
                value = isNegative ? -val : val;
                error = default;
                return true;
            }
        }

        if (Utf8Parser.TryParse(field, out value, out int bytesConsumed) && bytesConsumed == field.Length)
        {
            error = default;
            return true;
        }

        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidInteger, field.ToArray());
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetDouble(int index, string columnName, out double value, out CsvError error)
    {
        ReadOnlySpan<byte> span = GetRawField(index);

        long integerPart = 0;
        long fractionPart = 0;
        int fractionDigits = 0;
        bool hasDecimal = false;
        bool isValid = true;
        byte decimalSeparator = Options.AllowCommaDecimalSeparator ? (byte)',' : (byte)'.';

        for (int i = 0; i < span.Length; i++)
        {
            byte c = span[i];
            if (c >= (byte)'0' && c <= (byte)'9')
            {
                if (hasDecimal) { fractionPart = (fractionPart * 10) + (c - (byte)'0'); fractionDigits++; }
                else { integerPart = (integerPart * 10) + (c - (byte)'0'); }
            }
            else if ((c == (byte)'.' || c == decimalSeparator) && !hasDecimal) { hasDecimal = true; }
            else { isValid = false; break; }
        }

        if (isValid && fractionDigits <= 10)
        {
            value = integerPart + (fractionPart / Math.Pow(10, fractionDigits));
            error = default;
            return true;
        }

        Span<byte> buffer = stackalloc byte[Math.Min(span.Length, 64)];
        ReadOnlySpan<byte> normalizedSpan = span.Length <= 64 && Options.AllowCommaDecimalSeparator ? NormalizeDecimalSpan(span, buffer) : span;

        if (Utf8Parser.TryParse(normalizedSpan, out value, out int bytesConsumed) && bytesConsumed == normalizedSpan.Length)
        {
            error = default;
            return true;
        }

        value = default;
        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidFloat, span.ToArray());
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSingle(int index, string columnName, out float value, out CsvError error)
    {
        ReadOnlySpan<byte> span = GetRawField(index);

        if (Options.AllowCommaDecimalSeparator)
        {
            if (span.IndexOf((byte)',') < 0)
            {
                if (Utf8Parser.TryParse(span, out value, out int consumed1) && consumed1 == span.Length)
                {
                    error = default; return true;
                }
            }
            else
            {
                Span<byte> buffer = stackalloc byte[Math.Min(span.Length, 64)];
                ReadOnlySpan<byte> normalizedSpan = span.Length <= 64 ? NormalizeDecimalSpan(span, buffer) : span;

                if (Utf8Parser.TryParse(normalizedSpan, out value, out int consumed2) && consumed2 == normalizedSpan.Length)
                {
                    error = default; return true;
                }
            }
        }
        else if (Utf8Parser.TryParse(span, out value, out int consumed) && consumed == span.Length)
        {
            error = default; return true;
        }

        value = default;
        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidFloat, span.ToArray());
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetDecimal(int index, string columnName, out decimal value, out CsvError error)
    {
        ReadOnlySpan<byte> span = GetRawField(index);

        if (Options.AllowCommaDecimalSeparator)
        {
            if (span.IndexOf((byte)',') < 0)
            {
                if (Utf8Parser.TryParse(span, out value, out int consumed1) && consumed1 == span.Length)
                {
                    error = default; return true;
                }
            }
            else
            {
                Span<byte> buffer = stackalloc byte[Math.Min(span.Length, 64)];
                ReadOnlySpan<byte> normalizedSpan = span.Length <= 64 ? NormalizeDecimalSpan(span, buffer) : span;

                if (Utf8Parser.TryParse(normalizedSpan, out value, out int consumed2) && consumed2 == normalizedSpan.Length)
                {
                    error = default; return true;
                }
            }
        }
        else if (Utf8Parser.TryParse(span, out value, out int consumed) && consumed == span.Length)
        {
            error = default; return true;
        }

        value = default;
        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidDecimal, span.ToArray());
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetBoolean(int index, string? columnName, ReadOnlySpan<string> customTrueValues, ReadOnlySpan<string> customFalseValues, out bool value, out CsvError error)
    {
        ReadOnlySpan<byte> field = GetRawField(index);

        bool hasCustomFilter = !customTrueValues.IsEmpty || !customFalseValues.IsEmpty;
        var trueList = hasCustomFilter ? customTrueValues : Options.TrueValues.AsSpan();
        var falseList = hasCustomFilter ? customFalseValues : Options.FalseValues.AsSpan();

        foreach (var candidate in trueList)
        {
            if (EqualsIgnoreCaseUtf8(field, candidate))
            {
                value = true; 
                error = default; 
                return true;
            }
        }

        if (hasCustomFilter && customFalseValues.IsEmpty && !customTrueValues.IsEmpty)
        {
            value = false; error = default; return true;
        }

        foreach (var candidate in falseList)
        {
            if (EqualsIgnoreCaseUtf8(field, candidate))
            {
                value = false; error = default; return true;
            }
        }

        if (hasCustomFilter && customTrueValues.IsEmpty && !customFalseValues.IsEmpty)
        {
            value = true; error = default; return true;
        }

        value = default;
        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidBoolean, field.ToArray());
        return false;
    }

    public bool TryGetJson<T>(int index, string? columnName, JsonTypeInfo<T>? jsonTypeInfo, out T? value, out CsvError error)
    {
        ReadOnlySpan<byte> field = GetRawField(index);
        try
        {
            // Utilise GetString qui gère parfaitement le déséchappement des doubles guillemets CSV !
            string jsonString = GetString(field);

            if (jsonTypeInfo != null) 
                value = JsonSerializer.Deserialize(jsonString, jsonTypeInfo);
            else
                value = JsonSerializer.Deserialize<T>(jsonString);

            error = default;
            return true;
        }
        catch (Exception)
        {
            value = default;
            error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidJson, field.ToArray());
            return false;
        }
    }

    public bool TryGetDateTime(int index, string? columnName, out DateTime value, out CsvError error)
    {
        ReadOnlySpan<byte> field = GetRawField(index);

        if (field.Length == 28 || field.Length == 33)
        {
            if (field[4] == '-' && field[7] == '-' && field[10] == 'T' && field[13] == ':' && field[16] == ':')
            {
                int year = ((field[0] - '0') * 1000) + ((field[1] - '0') * 100) + ((field[2] - '0') * 10) + (field[3] - '0');
                int month = ((field[5] - '0') * 10) + (field[6] - '0');
                int day = ((field[8] - '0') * 10) + (field[9] - '0');
                int hour = ((field[11] - '0') * 10) + (field[12] - '0');
                int minute = ((field[14] - '0') * 10) + (field[15] - '0');
                int second = ((field[17] - '0') * 10) + (field[18] - '0');

                try
                {
                    value = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                    error = default; return true;
                }
                catch { }
            }
        }

        if (Utf8Parser.TryParse(field, out value, out int bytesConsumed, 'O') && bytesConsumed == field.Length)
        {
            error = default; return true;
        }
        if (Utf8Parser.TryParse(field, out value, out bytesConsumed) && bytesConsumed == field.Length)
        {
            error = default; return true;
        }

        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidDate, field.ToArray());
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetDateTime(int index, string columnName, string? localFormat, out DateTime value, out CsvError error)
    {
        ReadOnlySpan<byte> span = GetRawField(index);
        if (span.IsEmpty)
        {
            value = default;
            error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidDateTime, ReadOnlyMemory<byte>.Empty);
            return false;
        }

        // Fast-path ISO 8601 direct en UTF-8 sans aucune conversion en char[]
        if (string.IsNullOrEmpty(localFormat) && string.IsNullOrEmpty(Options.DateTimeFormat))
        {
            if (Utf8Parser.TryParse(span, out value, out int bytesConsumed, 'O') && bytesConsumed == span.Length)
            {
                error = default;
                return true;
            }
            if (Utf8Parser.TryParse(span, out value, out bytesConsumed) && bytesConsumed == span.Length)
            {
                error = default;
                return true;
            }
        }

        // Repli lent uniquement si un format personnalisé est exigé
        Span<char> chars = stackalloc char[Math.Min(span.Length, 64)];
        int charCount = Encoding.UTF8.GetChars(span, chars);
        ReadOnlySpan<char> charSpan = chars.Slice(0, charCount);

        if (!string.IsNullOrEmpty(localFormat) && DateTime.TryParseExact(charSpan, localFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = default; return true;
        }

        string? globalFormat = Options.DateTimeFormat;
        if (!string.IsNullOrEmpty(globalFormat) && DateTime.TryParseExact(charSpan, globalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = default; return true;
        }

        if (DateTime.TryParse(charSpan, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = default; return true;
        }

        value = default;
        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidDateTime, span.ToArray());
        return false;
    }

    public bool TryGetDateOnly(int index, string columnName, string? localFormat, out DateOnly value, out CsvError error)
    {
        ReadOnlySpan<byte> span = GetRawField(index);
        if (span.IsEmpty)
        {
            value = default;
            error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidDateTime, ReadOnlyMemory<byte>.Empty);
            return false;
        }

        Span<char> chars = stackalloc char[Math.Min(span.Length, 32)];
        int charCount = Encoding.UTF8.GetChars(span, chars);
        ReadOnlySpan<char> charSpan = chars.Slice(0, charCount);

        if (!string.IsNullOrEmpty(localFormat))
        {
            if (DateOnly.TryParseExact(charSpan, localFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                error = default; return true;
            }
        }

        string? globalFormat = Options.DateTimeFormat;
        if (!string.IsNullOrEmpty(globalFormat) && globalFormat != localFormat)
        {
            if (DateOnly.TryParseExact(charSpan, globalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                error = default; return true;
            }
        }

        if (DateOnly.TryParse(charSpan, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = default; return true;
        }

        value = default;
        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidDateTime, span.ToArray());
        return false;
    }

    public bool TryGetTimeOnly(int index, string columnName, string? localFormat, out TimeOnly value, out CsvError error)
    {
        ReadOnlySpan<byte> span = GetRawField(index);
        if (span.IsEmpty)
        {
            value = default;
            error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidDateTime, ReadOnlyMemory<byte>.Empty);
            return false;
        }

        Span<char> chars = stackalloc char[Math.Min(span.Length, 32)];
        int charCount = Encoding.UTF8.GetChars(span, chars);
        ReadOnlySpan<char> charSpan = chars.Slice(0, charCount);

        if (!string.IsNullOrEmpty(localFormat))
        {
            if (TimeOnly.TryParseExact(charSpan, localFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                error = default; return true;
            }
        }

        string? globalFormat = Options.DateTimeFormat;
        if (!string.IsNullOrEmpty(globalFormat) && globalFormat != localFormat)
        {
            if (TimeOnly.TryParseExact(charSpan, globalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                error = default; return true;
            }
        }

        if (TimeOnly.TryParse(charSpan, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = default; return true;
        }

        value = default;
        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidDateTime, span.ToArray());
        return false;
    }

    public bool TryGetGuid(int index, string? columnName, out Guid value, out CsvError error)
    {
        ReadOnlySpan<byte> field = GetRawField(index);

        if (Utf8Parser.TryParse(field, out value, out int bytesConsumed) && bytesConsumed == field.Length)
        {
            error = default; return true;
        }

        error = new CsvError(LineNumber, index, columnName, CsvErrorCode.InvalidGuid, field.ToArray());
        return false;
    }

    public string GetString(ReadOnlySpan<byte> utf8Span)
    {
        if (utf8Span.IsEmpty) return string.Empty;

        // Déséchappement manuel hautement optimisé (ex: "" devient ")
        if (utf8Span.IndexOf(Options.Quote) != -1)
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(utf8Span.Length);
            int unescapedLength = 0;
            for (int i = 0; i < utf8Span.Length; i++)
            {
                rented[unescapedLength++] = utf8Span[i];
                if (utf8Span[i] == Options.Quote && i + 1 < utf8Span.Length && utf8Span[i + 1] == Options.Quote)
                {
                    i++;
                }
            }
            string result = Options.UseStringPooling
                ? CsvStringPool.Shared.GetOrAdd(rented.AsSpan(0, unescapedLength))
                : System.Text.Encoding.UTF8.GetString(rented.AsSpan(0, unescapedLength));
            ArrayPool<byte>.Shared.Return(rented);
            return result;
        }

        return Options.UseStringPooling
            ? CsvStringPool.Shared.GetOrAdd(utf8Span) 
            : System.Text.Encoding.UTF8.GetString(utf8Span);
    }

    private static ReadOnlySpan<byte> TrimSpan(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return span;

        int start = 0;
        while (start < span.Length && (span[start] == (byte)' ' || span[start] == (byte)'\t')) start++;

        if (start == span.Length) return [];

        int end = span.Length - 1;
        while (end >= start && (span[end] == (byte)' ' || span[end] == (byte)'\t')) end--;

        return span.Slice(start, end - start + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EqualsIgnoreCaseUtf8(ReadOnlySpan<byte> span, string candidate)
    {
        if (span.Length != candidate.Length) return false;

        // Fast-path pour l'ASCII pur (très fréquent pour les null-values)
        for (int i = 0; i < span.Length; i++)
        {
            byte b = span[i];
            char c = candidate[i];
            if (b != c && (char.ToLowerInvariant((char)b) != char.ToLowerInvariant(c)))
                return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> NormalizeDecimalSpan(ReadOnlySpan<byte> source, Span<byte> stackBuffer)
    {
        if (!Options.AllowCommaDecimalSeparator) return source;

        int commaIdx = source.IndexOf((byte)',');
        if (commaIdx >= 0)
        {
            source.CopyTo(stackBuffer);
            stackBuffer[commaIdx] = (byte)'.';
            return stackBuffer;
        }

        return source;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFieldTooLarge(int index, out int fieldLength)
    {
        if ((uint)index < (uint)_fields.Length)
        {
            fieldLength = _fields[index].Length;
            return fieldLength > Options.MaxFieldSize;
        }

        fieldLength = 0;
        return false;
    }
}