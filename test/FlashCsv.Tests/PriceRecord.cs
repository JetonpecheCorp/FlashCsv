using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct PriceRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("price")]
    public double Price { get; init; }
}