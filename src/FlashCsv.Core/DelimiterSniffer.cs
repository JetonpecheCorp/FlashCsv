namespace FlashCsv.Core;

/// <summary>
/// Détection automatiquement du séparateur de colonnes d'un flux CSV.
/// </summary>
internal static class DelimiterSniffer
{
    private const int MaxSampleSize = 8 * 1024; // Analyse au maximum les 8 premiers Ko
    private const int MaxLinesToInspect = 5;

    /// <summary>
    /// Tente de déterminer le délimiteur le plus probable parmi une liste de candidats à partir d'un échantillon d'octets.
    /// </summary>
    /// <param name="sample">Extrait d'octets prélevé au début du document CSV.</param>
    /// <param name="candidates">Liste des octets candidats à tester (ex. virgule, point-virgule, tabulation).</param>
    /// <param name="quote">Caractère de guillemet à ignorer lors du décompte.</param>
    /// <param name="detectedDelimiter">Octet du séparateur identifié avec certitude.</param>
    /// <returns><see langword="true"/> si un séparateur cohérent a été identifié ; sinon <see langword="false"/>.</returns>
    public static bool TryDetect(
        ReadOnlySpan<byte> sample,
        ReadOnlySpan<byte> candidates,
        byte quote,
        out byte detectedDelimiter)
    {
        detectedDelimiter = default;

        if (sample.IsEmpty || candidates.IsEmpty)
            return false;

        ReadOnlySpan<byte> slice = sample.Length > MaxSampleSize ? sample[..MaxSampleSize] : sample;

        Span<int> bestCounts = stackalloc int[candidates.Length];
        Span<bool> isConsistent = stackalloc bool[candidates.Length];
        isConsistent.Fill(true);

        int linesCounted = 0;
        int lineStart = 0;
        bool inQuotes = false;

        Span<int> currentLineCounts = stackalloc int[candidates.Length];

        for (int i = 0; i < slice.Length && linesCounted < MaxLinesToInspect; i++)
        {
            byte b = slice[i];

            if (b == quote)
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes)
                continue;

            // Décompte des candidats sur la ligne courante
            for (int c = 0; c < candidates.Length; c++)
            {
                if (b == candidates[c])
                {
                    currentLineCounts[c]++;
                }
            }

            // Fin de ligne atteinte
            if (b == (byte)'\n' || i == slice.Length - 1)
            {
                linesCounted++;

                for (int c = 0; c < candidates.Length; c++)
                {
                    if (linesCounted == 1)
                    {
                        bestCounts[c] = currentLineCounts[c];
                    }
                    else if (bestCounts[c] != currentLineCounts[c])
                    {
                        // Incohérence de nombre de colonnes d'une ligne à l'autre
                        isConsistent[c] = false;
                    }
                }

                currentLineCounts.Clear();
                lineStart = i + 1;
            }
        }

        // Sélection du candidat cohérent avec le compte le plus élevé (> 0)
        int highestCount = 0;
        int selectedIndex = -1;

        for (int c = 0; c < candidates.Length; c++)
        {
            if (isConsistent[c] && bestCounts[c] > highestCount)
            {
                highestCount = bestCounts[c];
                selectedIndex = c;
            }
        }

        if (selectedIndex != -1)
        {
            detectedDelimiter = candidates[selectedIndex];
            return true;
        }

        return false;
    }
}
