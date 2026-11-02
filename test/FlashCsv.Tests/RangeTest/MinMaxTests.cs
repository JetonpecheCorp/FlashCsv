using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.RangeTest;

[CsvSerializable]
public partial record struct MinMaxRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("quantity")]
    [CsvMin(1)] // Doit être >= 1
    public int Quantity { get; init; }

    [CsvName("discount_rate")]
    [CsvMax(0.5)] // Doit être <= 50%
    public double DiscountRate { get; init; }

    [CsvName("both")]
    [CsvMin(10)]
    [CsvMax(20)] // Combinable également !
    public int BothBounds { get; init; }
}

public class MinMaxTests
{
    [Fact]
    public async Task Accepts_Valid_Min_And_Max()
    {
        string csv = 
            "id,quantity,discount_rate,both\r\n" +
            "1,10,0.25,15\r\n" +
            "2,1,0.5,10\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<MinMaxRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<MinMaxRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Equal(2, list.Count);
        Assert.Equal(10, list[0].Quantity);
        Assert.Equal(0.25, list[0].DiscountRate);
        Assert.Equal(15, list[0].BothBounds);
    }

    [Fact]
    public async Task Rejects_Below_Min()
    {
        string csv = 
            "id,quantity,discount_rate,both\r\n" +
            "1,0,0.1,15\r\n"; // quantity = 0 < 1

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<MinMaxRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<MinMaxRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.ValueOutOfRange, error.Value.Code);
        Assert.Equal("Quantity", error.Value.ColumnName);
    }

    [Fact]
    public async Task Rejects_Above_Max()
    {
        string csv = 
            "id,quantity,discount_rate,both\r\n" +
            "1,5,0.75,15\r\n"; // discount_rate = 0.75 > 0.5

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<MinMaxRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<MinMaxRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.ValueOutOfRange, error.Value.Code);
        Assert.Equal("DiscountRate", error.Value.ColumnName);
    }
}
