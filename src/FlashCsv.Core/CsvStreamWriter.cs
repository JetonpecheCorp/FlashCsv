using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FlashCsv.Annotations;

namespace FlashCsv.Core;

public delegate bool FieldFormatter<T>(T value, Span<byte> destination, out int bytesWritten, out CsvErrorCode errorCode);

/// <summary>
/// Écrivain CSV asynchrone ultra haute performance formatant directement en UTF-8 avec 0 allocation mémoire (AOT Ready).
/// </summary>
public sealed class CsvStreamWriter : IAsyncDisposable
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private bool _bomWritten;
    private readonly Stream _stream;
    public CsvWriterOptions Options { get; }
    private readonly bool _leaveOpen;
    private readonly SearchValues<byte> _escapeChars;
    private readonly byte[] _rentedBuffer;
    private int _bufferPosition;
    private bool _isDisposed;
    private bool _isFirstColumnInRow = true;

    public CsvStreamWriter(Stream stream, CsvWriterOptions? options = null, bool leaveOpen = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        Options = options ?? new CsvWriterOptions();
        _leaveOpen = leaveOpen;

        _rentedBuffer = ArrayPool<byte>.Shared.Rent(Options.BufferSize);
        _bufferPosition = 0;
        _escapeChars = SearchValues.Create([Options.Delimiter, Options.Quote, (byte)'\r', (byte)'\n']);
    }

    // =========================================================================================
    // GESTION DU DÉLIMITEUR (FAST-PATH)
    // =========================================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ValueTask PrepareColumnSeparatorFastAsync(CancellationToken ct)
    {
        if (_isFirstColumnInRow)
        {
            _isFirstColumnInRow = false;
            return ValueTask.CompletedTask;
        }

        if (_bufferPosition < _rentedBuffer.Length)
        {
            _rentedBuffer[_bufferPosition++] = Options.Delimiter;
            return ValueTask.CompletedTask;
        }

        return FlushAndWriteDelimiterAsync(ct);
    }

    private async ValueTask FlushAndWriteDelimiterAsync(CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        _rentedBuffer[_bufferPosition++] = Options.Delimiter;
    }

    private async ValueTask PrepareColumnSeparatorAsync(CancellationToken ct)
    {
        await PrepareColumnSeparatorFastAsync(ct).ConfigureAwait(false);
    }

    // =========================================================================================
    // ÉCRITURE DES PRIMITIVES (IUtf8SpanFormattable - Zéro Allocation)
    // =========================================================================================

    // --- INT32 ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(int value, CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, ct);

        if (_bufferPosition + 16 > _rentedBuffer.Length) return FlushAndWriteIntAsync(value, ct);

        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
        return ValueTask.CompletedTask;
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, string text, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        await WriteStringWithoutSeparatorAsync(text, ct).ConfigureAwait(false);
    }

    private async ValueTask FlushAndWriteStringAsync(string text, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        await WriteStringWithoutSeparatorAsync(text, ct).ConfigureAwait(false);
    }

    private async ValueTask WriteStringWithoutSeparatorAsync(string text, CancellationToken ct)
    {
        int maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);
        if (_bufferPosition + maxBytes + 2 > _rentedBuffer.Length)
        {
            await FlushAsync(ct).ConfigureAwait(false);
        }

        int actualBytes = Encoding.UTF8.GetBytes(text.AsSpan(), _rentedBuffer.AsSpan(_bufferPosition));
        if (Options.QuoteStrategy == QuoteStrategy.Always || 
            (Options.QuoteStrategy == QuoteStrategy.Minimal && _rentedBuffer.AsSpan(_bufferPosition, actualBytes).IndexOfAny(_escapeChars) >= 0))
        {
            await WriteStringWithQuotesAsync(text, ct).ConfigureAwait(false);
        }
        else
        {
            _bufferPosition += actualBytes;
        }
    }

    private async ValueTask WriteStringWithQuotesAsync(string text, CancellationToken ct)
    {
        int maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);
        byte[] rented = ArrayPool<byte>.Shared.Rent(maxBytes);
        try
        {
            int actualBytes = Encoding.UTF8.GetBytes(text.AsSpan(), rented.AsSpan());
            await WriteUtf8MemoryWithEscapingAsync(rented.AsMemory(0, actualBytes), ct).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask WriteFieldAsync(string? value, bool sanitizeFormula, CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            await WriteEmptyFieldAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if ((sanitizeFormula || Options.EscapeFormulas) && StartsWithFormulaTrigger(value.AsSpan()))
        {
            await WriteFieldAsync("'" + value, cancellationToken).ConfigureAwait(false);
            return;
        }

        await WriteFieldAsync(value, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask FlushAndWriteIntAsync(int value, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
    }

    // --- INT64 ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(long value, CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, ct);

        if (_bufferPosition + 32 > _rentedBuffer.Length) return FlushAndWriteLongAsync(value, ct);

        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
        return ValueTask.CompletedTask;
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, long value, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        if (_bufferPosition + 32 > _rentedBuffer.Length)
        {
            await FlushAndWriteLongAsync(value, ct).ConfigureAwait(false);
        }
        else
        {
            value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
            _bufferPosition += bytesWritten;
        }
    }

    private async ValueTask FlushAndWriteLongAsync(long value, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
    }

    // --- DOUBLE ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(double value, CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, ct);

        if (_bufferPosition + 32 > _rentedBuffer.Length) return FlushAndWriteDoubleAsync(value, ct);

        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
        return ValueTask.CompletedTask;
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, double value, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        if (_bufferPosition + 32 > _rentedBuffer.Length)
        {
            await FlushAndWriteDoubleAsync(value, ct).ConfigureAwait(false);
        }
        else
        {
            value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
            _bufferPosition += bytesWritten;
        }
    }

    private async ValueTask FlushAndWriteDoubleAsync(double value, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
    }

    // --- SINGLE (FLOAT) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(float value, CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, ct);

        if (_bufferPosition + 32 > _rentedBuffer.Length) return FlushAndWriteFloatAsync(value, ct);

        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
        return ValueTask.CompletedTask;
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, float value, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        if (_bufferPosition + 32 > _rentedBuffer.Length)
        {
            await FlushAndWriteFloatAsync(value, ct).ConfigureAwait(false);
        }
        else
        {
            value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
            _bufferPosition += bytesWritten;
        }
    }

    private async ValueTask FlushAndWriteFloatAsync(float value, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
    }

    // --- DECIMAL ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(decimal value, CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, ct);

        if (_bufferPosition + 64 > _rentedBuffer.Length) return FlushAndWriteDecimalAsync(value, ct);

        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
        return ValueTask.CompletedTask;
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, decimal value, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        if (_bufferPosition + 64 > _rentedBuffer.Length)
        {
            await FlushAndWriteDecimalAsync(value, ct).ConfigureAwait(false);
        }
        else
        {
            value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
            _bufferPosition += bytesWritten;
        }
    }

    private async ValueTask FlushAndWriteDecimalAsync(decimal value, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, default, System.Globalization.CultureInfo.InvariantCulture);
        _bufferPosition += bytesWritten;
    }

    // --- GUID ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(Guid value, string? format = "D", CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, format, ct);

        if (_bufferPosition + 40 > _rentedBuffer.Length) return FlushAndWriteGuidAsync(value, format, ct);

        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format);
        _bufferPosition += bytesWritten;
        return ValueTask.CompletedTask;
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, Guid value, string? format, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        if (_bufferPosition + 40 > _rentedBuffer.Length)
        {
            await FlushAndWriteGuidAsync(value, format, ct).ConfigureAwait(false);
        }
        else
        {
            value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format);
            _bufferPosition += bytesWritten;
        }
    }

    private async ValueTask FlushAndWriteGuidAsync(Guid value, string? format, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format);
        _bufferPosition += bytesWritten;
    }

    // --- DATETIME ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(DateTime value, string? format = null, CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, format, ct);

        if (_bufferPosition + 128 > _rentedBuffer.Length) return FlushAndWriteDateTimeAsync(value, format, ct);

        if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
        {
            _bufferPosition += bytesWritten;
            return ValueTask.CompletedTask;
        }
        return WriteFieldFallbackAsync(value, format, ct);
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, DateTime value, string? format, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        if (_bufferPosition + 128 > _rentedBuffer.Length)
        {
            await FlushAndWriteDateTimeAsync(value, format, ct).ConfigureAwait(false);
        }
        else
        {
            if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                _bufferPosition += bytesWritten;
                return;
            }
            await WriteFieldFallbackAsync(value, format, ct).ConfigureAwait(false);
        }
    }

    private async ValueTask FlushAndWriteDateTimeAsync(DateTime value, string? format, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
        {
            _bufferPosition += bytesWritten;
            return;
        }
        await WriteFieldFallbackAsync(value, format, ct).ConfigureAwait(false);
    }

    private async ValueTask WriteFieldFallbackAsync(DateTime value, string? format, CancellationToken ct)
    {
        string fallback = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        await WriteStringWithoutSeparatorAsync(fallback, ct).ConfigureAwait(false);
    }

    // --- DATEONLY ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(DateOnly value, string? format = null, CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, format, ct);

        if (_bufferPosition + 128 > _rentedBuffer.Length) return FlushAndWriteDateOnlyAsync(value, format, ct);

        if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
        {
            _bufferPosition += bytesWritten;
            return ValueTask.CompletedTask;
        }
        return WriteFieldFallbackAsync(value, format, ct);
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, DateOnly value, string? format, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        if (_bufferPosition + 128 > _rentedBuffer.Length)
        {
            await FlushAndWriteDateOnlyAsync(value, format, ct).ConfigureAwait(false);
        }
        else
        {
            if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                _bufferPosition += bytesWritten;
                return;
            }
            await WriteFieldFallbackAsync(value, format, ct).ConfigureAwait(false);
        }
    }

    private async ValueTask FlushAndWriteDateOnlyAsync(DateOnly value, string? format, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
        {
            _bufferPosition += bytesWritten;
            return;
        }
        await WriteFieldFallbackAsync(value, format, ct).ConfigureAwait(false);
    }

    private async ValueTask WriteFieldFallbackAsync(DateOnly value, string? format, CancellationToken ct)
    {
        string fallback = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        await WriteStringWithoutSeparatorAsync(fallback, ct).ConfigureAwait(false);
    }

    // --- TIMEONLY ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(TimeOnly value, string? format = null, CancellationToken ct = default)
    {
        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, value, format, ct);

        if (_bufferPosition + 64 > _rentedBuffer.Length) return FlushAndWriteTimeOnlyAsync(value, format, ct);

        if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
        {
            _bufferPosition += bytesWritten;
            return ValueTask.CompletedTask;
        }
        return WriteFieldFallbackAsync(value, format, ct);
    }

    private async ValueTask WriteFieldWithAsyncSeparator(ValueTask sepTask, TimeOnly value, string? format, CancellationToken ct)
    {
        await sepTask.ConfigureAwait(false);
        if (_bufferPosition + 64 > _rentedBuffer.Length)
        {
            await FlushAndWriteTimeOnlyAsync(value, format, ct).ConfigureAwait(false);
        }
        else
        {
            if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                _bufferPosition += bytesWritten;
                return;
            }
            await WriteFieldFallbackAsync(value, format, ct).ConfigureAwait(false);
        }
    }

    private async ValueTask FlushAndWriteTimeOnlyAsync(TimeOnly value, string? format, CancellationToken ct)
    {
        await FlushAsync(ct).ConfigureAwait(false);
        if (value.TryFormat(_rentedBuffer.AsSpan(_bufferPosition), out int bytesWritten, format, System.Globalization.CultureInfo.InvariantCulture))
        {
            _bufferPosition += bytesWritten;
            return;
        }
        await WriteFieldFallbackAsync(value, format, ct).ConfigureAwait(false);
    }

    private async ValueTask WriteFieldFallbackAsync(TimeOnly value, string? format, CancellationToken ct)
    {
        string fallback = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        await WriteStringWithoutSeparatorAsync(fallback, ct).ConfigureAwait(false);
    }

    // --- BOOLEAN ---
    public async ValueTask WriteFieldAsync(bool value, CancellationToken cancellationToken = default)
    {
        await PrepareColumnSeparatorAsync(cancellationToken).ConfigureAwait(false);
        ReadOnlyMemory<byte> literal = value ? "true"u8.ToArray() : "false"u8.ToArray();
        await WriteRawBytesMemoryAsync(literal, cancellationToken).ConfigureAwait(false);
    }

    // =========================================================================================
    // TEXTES, JSON, CUSTOM ET STRUCTURE
    // =========================================================================================

    public async ValueTask WriteHeaderAsync(ReadOnlyMemory<string> headers, CancellationToken cancellationToken = default)
    {
        // Écriture du BOM UTF-8 avant la première ligne
        if (Options.WriteBom && !_bomWritten)
        {
            await WriteRawBytesMemoryAsync(Utf8Bom, cancellationToken).ConfigureAwait(false);
            _bomWritten = true;
        }

        for (int i = 0; i < headers.Length; i++)
        {
            string headerName = headers.Span[i];
            if (Options.FormatHeader != null) headerName = Options.FormatHeader(headerName) ?? headerName;
            await WriteFieldAsync(headerName.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        await WriteEndOfLineAsync(cancellationToken).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteFieldAsync(string? text, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(text)) return WriteEmptyFieldAsync(ct);

        var sepTask = PrepareColumnSeparatorFastAsync(ct);
        if (!sepTask.IsCompletedSuccessfully) return WriteFieldWithAsyncSeparator(sepTask, text, ct);

        int maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);
        if (_bufferPosition + maxBytes + 2 > _rentedBuffer.Length)
            return FlushAndWriteStringAsync(text, ct);

        int actualBytes = Encoding.UTF8.GetBytes(text.AsSpan(), _rentedBuffer.AsSpan(_bufferPosition));

        if (Options.QuoteStrategy == QuoteStrategy.Always || 
            (Options.QuoteStrategy == QuoteStrategy.Minimal && _rentedBuffer.AsSpan(_bufferPosition, actualBytes).IndexOfAny(_escapeChars) >= 0))
        {
            return WriteStringWithQuotesAsync(text, ct); 
        }

        _bufferPosition += actualBytes;
        return ValueTask.CompletedTask;
    }

    public async ValueTask WriteFieldAsync(ReadOnlyMemory<char> text, CancellationToken cancellationToken = default)
    {
        await PrepareColumnSeparatorAsync(cancellationToken).ConfigureAwait(false);

        if (text.IsEmpty)
        {
            if (Options.QuoteStrategy == QuoteStrategy.Always)
            {
                if (!EnsureCapacitySync(2)) await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
                _rentedBuffer[_bufferPosition++] = Options.Quote;
                _rentedBuffer[_bufferPosition++] = Options.Quote;
            }
            return;
        }

        int maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);
        byte[] rented = ArrayPool<byte>.Shared.Rent(maxBytes);
        try
        {
            int actualBytes = Encoding.UTF8.GetBytes(text.Span, rented.AsSpan());
            await WriteUtf8MemoryWithEscapingAsync(rented.AsMemory(0, actualBytes), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public async ValueTask WriteJsonFieldAsync<T>(T value, JsonTypeInfo<T>? jsonTypeInfo = null, CancellationToken cancellationToken = default)
    {
        await PrepareColumnSeparatorAsync(cancellationToken).ConfigureAwait(false);

        var bufferWriter = new ArrayBufferWriter<byte>(256);
        await using (var utf8JsonWriter = new Utf8JsonWriter(bufferWriter))
        {
            if (jsonTypeInfo != null) JsonSerializer.Serialize(utf8JsonWriter, value, jsonTypeInfo);
            else JsonSerializer.Serialize(utf8JsonWriter, value);
        }
        await WriteUtf8MemoryWithEscapingAsync(bufferWriter.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask WriteCustomFieldAsync<T>(T value, FieldFormatter<T> formatter, CancellationToken cancellationToken = default)
    {
        await PrepareColumnSeparatorAsync(cancellationToken).ConfigureAwait(false);

        Span<byte> buffer = stackalloc byte[256];
        if (!formatter(value, buffer, out int written, out CsvErrorCode _))
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                if (formatter(value, rented, out written, out _))
                {
                    await WriteRawBytesMemoryAsync(rented.AsMemory(0, written), cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
        await WriteRawBytesMemoryAsync(buffer.Slice(0, written).ToArray(), cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask WriteEndOfLineAsync(CancellationToken cancellationToken = default)
    {
        await WriteRawBytesMemoryAsync(Options.NewLine.AsMemory(), cancellationToken).ConfigureAwait(false);
        _isFirstColumnInRow = true;
    }

    public ValueTask WriteEmptyFieldAsync(CancellationToken cancellationToken = default)
    {
        return PrepareColumnSeparatorAsync(cancellationToken);
    }

    // =========================================================================================
    // UTILITAIRES INTERNES ET GESTION MÉMOIRE
    // =========================================================================================

    private static bool StartsWithFormulaTrigger(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return false;
        char c = text[0];
        return c is '=' or '+' or '-' or '@' or '\t' or '\r';
    }

    private async ValueTask WriteUtf8MemoryWithEscapingAsync(ReadOnlyMemory<byte> sourceMemory, CancellationToken cancellationToken)
    {
        bool mustQuote = Options.QuoteStrategy == QuoteStrategy.Always ||
                         (Options.QuoteStrategy == QuoteStrategy.Minimal && sourceMemory.Span.IndexOfAny(_escapeChars) != -1);

        if (!mustQuote)
        {
            await WriteRawBytesMemoryAsync(sourceMemory, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!EnsureCapacitySync(1)) await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
        _rentedBuffer[_bufferPosition++] = Options.Quote;

        int current = 0;
        int length = sourceMemory.Length;

        while (current < length)
        {
            int quoteIdx = sourceMemory.Span[current..].IndexOf(Options.Quote);
            if (quoteIdx == -1)
            {
                await WriteRawBytesMemoryAsync(sourceMemory[current..], cancellationToken).ConfigureAwait(false);
                break;
            }

            if (quoteIdx > 0)
            {
                await WriteRawBytesMemoryAsync(sourceMemory.Slice(current, quoteIdx), cancellationToken).ConfigureAwait(false);
            }

            if (!EnsureCapacitySync(2)) await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
            _rentedBuffer[_bufferPosition++] = Options.Quote;
            _rentedBuffer[_bufferPosition++] = Options.Quote;

            current += quoteIdx + 1;
        }

        if (!EnsureCapacitySync(1)) await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
        _rentedBuffer[_bufferPosition++] = Options.Quote;
    }

    private async ValueTask WriteRawBytesMemoryAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
    {
        int copied = 0;
        while (copied < source.Length)
        {
            int available = _rentedBuffer.Length - _bufferPosition;
            if (available == 0)
            {
                await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
                available = _rentedBuffer.Length - _bufferPosition;
            }

            int toWrite = Math.Min(available, source.Length - copied);
            source.Span.Slice(copied, toWrite).CopyTo(_rentedBuffer.AsSpan(_bufferPosition));
            _bufferPosition += toWrite;
            copied += toWrite;
        }
    }

    private bool EnsureCapacitySync(int requiredBytes) => _bufferPosition + requiredBytes <= _rentedBuffer.Length;

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask FlushBufferAsync(CancellationToken cancellationToken)
    {
        if (_bufferPosition > 0)
        {
            await _stream.WriteAsync(_rentedBuffer.AsMemory(0, _bufferPosition), cancellationToken).ConfigureAwait(false);
            _bufferPosition = 0;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            try
            {
                await FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(_rentedBuffer);
                if (!_leaveOpen)
                {
                    await _stream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
