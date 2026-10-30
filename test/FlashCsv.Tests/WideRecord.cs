using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct WideRecord
{
    [CsvName("col_0")]
    public int Col0 { get; init; }

    [CsvName("col_1")]
    public string Col1 { get; init; }

    [CsvName("col_50")]
    public double Col50 { get; init; }

    [CsvName("col_64")]
    public int Col64 { get; init; } // Valide le seuil limite de 64

    [CsvName("col_99")]
    public string Col99 { get; init; } // Valide la 100e colonne (au-delà du stackalloc)
}
