using System.Buffers;
using System.Runtime.CompilerServices;
using FlashCsv.Annotations;

namespace FlashCsv.Core;

public sealed class CsvStreamReader : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly CsvReaderOptions _options;

    private byte[] _rentedBuffer;
    private int _bufferStart;
    private int _bufferEnd;
    private bool _isEndOfStream;

    private byte _delimiter;
    private SearchValues<byte> _delimsAndQuotes = null!;
    private long _currentLineNumber;
    private bool _isDisposed;
    private readonly byte[] _detectedNewLine = [(byte)'\r', (byte)'\n'];
    private (int Offset, int Length)[] _columnsBuffer; // Plus readonly !

    public string[]? Headers { get; private set; }
    public ReadOnlyMemory<byte> DetectedNewLine => _detectedNewLine;
    public long CurrentLineNumber => _currentLineNumber;
    public byte Delimiter => _delimiter;

    private CsvStreamReader(Stream stream, CsvReaderOptions options, bool leaveOpen)
    {
        _stream = stream;
        _options = options;
        _leaveOpen = leaveOpen;
        _rentedBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(options.BufferSize, 65536));
        _columnsBuffer = ArrayPool<(int Offset, int Length)>.Shared.Rent(64);
    }

    public static async ValueTask<CsvStreamReader> CreateAsync(Stream stream, CsvReaderOptions? options = null, bool leaveOpen = false, CancellationToken cancellationToken = default)
    {
        options ??= new CsvReaderOptions();
        var reader = new CsvStreamReader(stream, options, leaveOpen);
        await reader.InitializeDelimiterAsync(cancellationToken).ConfigureAwait(false);
        return reader;
    }

    private async ValueTask InitializeDelimiterAsync(CancellationToken cancellationToken)
    {
        await FillBufferAsync(cancellationToken).ConfigureAwait(false);

        if (_options.Delimiter.HasValue)
        {
            _delimiter = (byte)_options.Delimiter.Value;
        }
        else
        {
            ReadOnlySpan<byte> sample = _rentedBuffer.AsSpan(_bufferStart, Math.Min(_bufferEnd - _bufferStart, 8192));
            if (!DelimiterSniffer.TryDetect(sample, _options.CandidateDelimiters.Select(x => (byte)x).ToArray(), _options.Quote, out _delimiter))
            {
                _delimiter = (byte)',';
            }
        }

        _delimsAndQuotes = SearchValues.Create([_delimiter, _options.Quote, (byte)'\r', (byte)'\n']);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryReadNextRowSync<T>(int[] colMap, out bool success, out T value, out CsvError error) where T : ICsvSerializable<T>
    {
        var cols = _columnsBuffer;
        int end = _bufferEnd;
        int start = _bufferStart;
        byte quote = _options.Quote;
        byte delim = _delimiter;

        int colCount = 0;
        int cur = start;
        int cellStart = start;
        bool rowFound = false;

        ReadOnlySpan<byte> buffer = _rentedBuffer.AsSpan(0, end);

        while (cur < end)
        {
            // On borne strictement entre cur et end
            int idx = buffer.Slice(cur, end - cur).IndexOfAny(_delimsAndQuotes);
            if (idx < 0)
            {
                cur = end;
                break;
            }

            cur += idx;
            byte b = buffer[cur];

            if (b == delim)
            {
                EnsureColumnCapacity(ref colCount, ref cols);
                cols[colCount++] = (cellStart - start, cur - cellStart);
                cur++;
                cellStart = cur;
            }
            else if (b == (byte)'\n')
            {
                int len = cur - cellStart;
                if (len > 0 && buffer[cur - 1] == (byte)'\r') len--;

                EnsureColumnCapacity(ref colCount, ref cols);
                cols[colCount++] = (cellStart - start, len);

                rowFound = true;
                _bufferStart = cur + 1;
                break;
            }
            else if (b == quote)
            {
                cur++;
                while (cur < end)
                {
                    int nextQuote = buffer.Slice(cur, end - cur).IndexOf(quote);
                    if (nextQuote < 0) { cur = end; break; }
                    cur += nextQuote;
                    if (cur + 1 < end && buffer[cur + 1] == quote)
                    {
                        cur += 2;
                    }
                    else
                    {
                        break;
                    }
                }
                cur++;
            }
            else // '\r'
            {
                cur++;
            }
        }

        if (!rowFound)
        {
            if (_isEndOfStream && end > start)
            {
                EnsureColumnCapacity(ref colCount, ref cols);
                cols[colCount++] = (cellStart - start, end - cellStart);
                rowFound = true;
                _bufferStart = end;
            }
            else
            {
                success = false; value = default!; error = default; return false;
            }
        }

        _columnsBuffer = cols;
        _currentLineNumber++;
        var lineSpan = new ReadOnlySpan<byte>(_rentedBuffer, start, _bufferStart - start);
        if (lineSpan.Length > 0 && lineSpan[^1] == (byte)'\n') lineSpan = lineSpan[..^1];
        if (lineSpan.Length > 0 && lineSpan[^1] == (byte)'\r') lineSpan = lineSpan[..^1];

        var view = new CsvRowView(lineSpan, cols.AsSpan(0, colCount), _currentLineNumber, _options, Headers);
        success = T.TryDeserialize(view, colMap, out value, out error);
        return true;
    }

    public async ValueTask<bool> ReadMoreDataAsync(CancellationToken cancellationToken = default)
    {
        if (_isEndOfStream) return false;
        await FillBufferAsync(cancellationToken).ConfigureAwait(false);
        return !_isEndOfStream || (_bufferEnd - _bufferStart > 0);
    }

    private async ValueTask FillBufferAsync(CancellationToken cancellationToken)
    {
        if (_isEndOfStream) return;

        int remaining = _bufferEnd - _bufferStart;
        if (remaining > 0 && _bufferStart > 0)
        {
            _rentedBuffer.AsSpan(_bufferStart, remaining).CopyTo(_rentedBuffer);
        }
        _bufferStart = 0;
        _bufferEnd = remaining;

        if (_bufferEnd == _rentedBuffer.Length)
        {
            byte[] largerBuffer = ArrayPool<byte>.Shared.Rent(_rentedBuffer.Length * 2);
            Array.Copy(_rentedBuffer, 0, largerBuffer, 0, _bufferEnd);
            ArrayPool<byte>.Shared.Return(_rentedBuffer);
            _rentedBuffer = largerBuffer;
        }

        int bytesRead = await _stream.ReadAsync(_rentedBuffer.AsMemory(_bufferEnd, _rentedBuffer.Length - _bufferEnd), cancellationToken).ConfigureAwait(false);

        if (bytesRead == 0) _isEndOfStream = true;
        else _bufferEnd += bytesRead;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureColumnCapacity(ref int colCount, ref (int Offset, int Length)[] cols)
    {
        if (colCount >= cols.Length)
        {
            var larger = ArrayPool<(int Offset, int Length)>.Shared.Rent(cols.Length * 2);
            Array.Copy(cols, larger, cols.Length);
            ArrayPool<(int Offset, int Length)>.Shared.Return(cols);
            cols = larger;
        }
    }

    public async ValueTask<string[]> ReadHeaderAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            int lineEnd = -1;
            ReadOnlySpan<byte> span = _rentedBuffer.AsSpan(_bufferStart, _bufferEnd - _bufferStart);

            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == (byte)'\n')
                {
                    lineEnd = i;
                    break;
                }
            }

            if (lineEnd >= 0)
            {
                _currentLineNumber++;
                int rawLen = (lineEnd > 0 && span[lineEnd - 1] == (byte)'\r') ? lineEnd - 1 : lineEnd;
                var lineSpan = span.Slice(0, rawLen);
                _bufferStart += lineEnd + 1;

                var tokenizer = new CsvRowTokenizer(lineSpan, _delimiter, _options.Quote, SearchValues.Create([_delimiter, _options.Quote]));
                int colCount = 0;
                CsvErrorCode tokenError = CsvErrorCode.None;
                
                // Correction du parseur 100 colonnes
                while (tokenizer.TryReadNextField(out int offset, out int length, out tokenError))
                {
                    EnsureColumnCapacity(ref colCount, ref _columnsBuffer);
                    _columnsBuffer[colCount++] = (offset, length);
                }

                if (tokenError != CsvErrorCode.None) throw new InvalidOperationException($"Impossible de lire l'en-tête CSV : {tokenError}");

                var headers = new string[colCount];
                var view = new CsvRowView(lineSpan, _columnsBuffer.AsSpan(0, colCount), _currentLineNumber, _options);

                for (int i = 0; i < colCount; i++)
                {
                    var cell = view.GetRawField(i);
                    headers[i] = cell.IsEmpty ? string.Empty : System.Text.Encoding.UTF8.GetString(cell);
                }

                Headers = headers;
                return headers;
            }

            if (_isEndOfStream) throw new InvalidOperationException("Fichier CSV vide.");

            await FillBufferAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            ArrayPool<byte>.Shared.Return(_rentedBuffer);
            ArrayPool<(int Offset, int Length)>.Shared.Return(_columnsBuffer);
            if (!_leaveOpen) await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}