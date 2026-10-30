using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct ProductRecord
{
    [CsvName("sku")]
    public string Sku { get; init; }

    [CsvName("price")]
    [CsvConverter<MoneyConverter>()]
    public Money Price { get; init; }
}
