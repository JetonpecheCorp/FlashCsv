using System.Net;
using System.Text;
using FlashCsv.Annotations;

namespace FlashCsv.Core;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static class CsvSanitizer
{
    public static string Sanitize(string input, HtmlSanitizeMode mode)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return mode switch
        {
            HtmlSanitizeMode.StripTags => StripHtmlTags(input),
            _ => WebUtility.HtmlEncode(input)
        };
    }

    public static string ApplyCase(string input, CsvCaseMode mode)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return mode switch
        {
            CsvCaseMode.Upper => input.ToUpperInvariant(),
            CsvCaseMode.Lower => input.ToLowerInvariant(),
            CsvCaseMode.Title => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(input),
            _ => input
        };
    }

    /// <summary>
    /// Découpe une chaîne selon un délimiteur en respectant un éventuel caractère d'échappement.
    /// </summary>
    public static string[] SplitWithEscape(string input, char delimiter, char escapeChar)
    {
        if (string.IsNullOrEmpty(input))
            return [];

        // Si aucun caractère d'échappement n'est configuré ('\0')
        if (escapeChar == '\0')
        {
            return input.Split(delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var results = new List<string>();
        var sb = new StringBuilder(input.Length);
        bool isEscaped = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (isEscaped)
            {
                sb.Append(c);
                isEscaped = false;
            }
            else if (c == escapeChar)
            {
                isEscaped = true;
            }
            else if (c == delimiter)
            {
                string token = sb.ToString().Trim();
                if (token.Length > 0)
                {
                    results.Add(token);
                }
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        string lastToken = sb.ToString().Trim();
        if (lastToken.Length > 0)
        {
            results.Add(lastToken);
        }

        return [.. results];
    }

    /// <summary>
    /// Rejoint une collection d'éléments en échappant les occurrences du délimiteur et de l'échappement.
    /// </summary>
    public static string JoinWithEscape(IEnumerable<string> items, char delimiter, char escapeChar)
    {
        if (items == null) return string.Empty;

        // Sans échappement
        if (escapeChar == '\0')
        {
            return string.Join(delimiter, items);
        }

        string delStr = delimiter.ToString();
        string escStr = escapeChar.ToString();

        var escapedItems = items.Select(item =>
        {
            if (string.IsNullOrEmpty(item)) return item;
            // On double d'abord l'échappement, puis on protège le délimiteur
            return item.Replace(escStr, escStr + escStr)
                       .Replace(delStr, escStr + delStr);
        });

        return string.Join(delimiter, escapedItems);
    }

    private static string StripHtmlTags(string input)
    {
        if (!input.Contains('<'))
            return input;

        var sb = new StringBuilder(input.Length);
        bool insideTag = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '<')
            {
                insideTag = true;
                continue;
            }

            if (c == '>')
            {
                insideTag = false;
                continue;
            }

            if (!insideTag)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
