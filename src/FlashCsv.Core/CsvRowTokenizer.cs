using System.Buffers;
using FlashCsv.Annotations;

namespace FlashCsv.Core;

public ref struct CsvRowTokenizer
{
    private readonly ReadOnlySpan<byte> _row;
    private readonly byte _delimiter;
    private readonly byte _quote;
    private readonly SearchValues<byte> _specialChars;

    private int _cursor;

    public CsvRowTokenizer(
        ReadOnlySpan<byte> row, 
        byte delimiter, 
        byte quote, 
        SearchValues<byte> specialChars)
    {
        _row = row;
        _delimiter = delimiter;
        _quote = quote;
        _cursor = 0;
        _specialChars = specialChars;
    }

    public int TokenizeAll(Span<(int Offset, int Length)> destination, out CsvErrorCode error)
    {
        int colCount = 0;
        error = CsvErrorCode.None;

        while (TryReadNextField(out int offset, out int length, out error))
        {
            if (colCount < destination.Length)
            {
                destination[colCount] = (offset, length);
            }
            colCount++;
        }

        return error != CsvErrorCode.None ? -1 : colCount;
    }

    public bool TryReadNextField(out int offset, out int length, out CsvErrorCode error)
    {
        error = CsvErrorCode.None;

        // Si on a déjà dépassé la ligne, arrêt net sans Slice
        if (_cursor > _row.Length)
        {
            offset = 0;
            length = 0;
            return false;
        }

        if (_cursor == _row.Length)
        {
            // Fin de ligne avec délimiteur terminal (ex: "a,b,")
            offset = _cursor;
            length = 0;
            _cursor++; // Passe à _row.Length + 1 pour stopper la lecture au prochain tour
            return true;
        }

        // Cas 1 : Champ échappé avec guillemets (RFC 4180)
        if (_row[_cursor] == _quote)
        {
            return ParseQuotedField(out offset, out length, out error);
        }

        // Cas 2 : Champ non échappé standard
        ReadOnlySpan<byte> slice = _row.Slice(_cursor);
        int idx = slice.IndexOfAny(_specialChars);

        if (idx == -1)
        {
            // Dernier champ de la ligne
            offset = _cursor;
            length = slice.Length;
            _cursor = _row.Length + 1; // Fin complète
            return true;
        }

        if (slice[idx] == _delimiter)
        {
            offset = _cursor;
            length = idx;
            _cursor += idx + 1;
            return true;
        }

        // Guillemet mal placé au milieu d'un champ non quoté
        error = CsvErrorCode.InvalidFormat;
        offset = _cursor;
        length = idx;
        return false;
    }

    private bool ParseQuotedField(out int offset, out int length, out CsvErrorCode error)
    {
        error = CsvErrorCode.None;
        _cursor++; // Passe le guillemet ouvrant
        offset = _cursor;
        int start = _cursor;

        while (_cursor <= _row.Length)
        {
            if (_cursor == _row.Length)
            {
                error = CsvErrorCode.UnterminatedQuote;
                length = 0;
                return false;
            }

            int quoteIdx = _row.Slice(_cursor).IndexOf(_quote);
            if (quoteIdx == -1)
            {
                error = CsvErrorCode.UnterminatedQuote;
                length = 0;
                return false;
            }

            _cursor += quoteIdx;

            // Gestion du double guillemet d'échappement ("")
            if (_cursor + 1 < _row.Length && _row[_cursor + 1] == _quote)
            {
                _cursor += 2;
                continue;
            }

            // Guillemet fermant trouvé
            length = _cursor - start;
            _cursor++; // Passe le guillemet fermant

            if (_cursor < _row.Length)
            {
                if (_row[_cursor] == _delimiter)
                {
                    _cursor++; // Saute le délimiteur
                }
                else
                {
                    // Saute les éventuels espaces parasites jusqu'au délimiteur suivant
                    int nextDelim = _row.Slice(_cursor).IndexOf(_delimiter);
                    if (nextDelim == -1)
                    {
                        _cursor = _row.Length + 1;
                    }
                    else
                    {
                        _cursor += nextDelim + 1;
                    }
                }
            }
            else
            {
                // Fin de chaîne atteinte juste après le guillemet fermant
                _cursor = _row.Length + 1;
            }

            return true;
        }

        error = CsvErrorCode.UnterminatedQuote;
        length = 0;
        return false;
    }
}