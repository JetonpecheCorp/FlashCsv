using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests;

public class DoubleParseTest
{
    [Fact]
    public async Task Accepts_Both_Dot_And_Comma_By_Default()
    {
        // Séparateur ';' pour éviter tout conflit avec les virgules décimales non quotées
        string csv = 
            "id;price\r\n" +
            "1;12.5\r\n" +
            "2;12,5\r\n" +
            "3;0,99\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var options = new CsvReaderOptions { Delimiter = ';' };
        var rows = new List<PriceRecord>();

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<PriceRecord>(stream, options))
        {
            rows.Add(row);
        }

        Assert.Equal(3, rows.Count);
        Assert.Equal(12.5, rows[0].Price);
        Assert.Equal(12.5, rows[1].Price);
        Assert.Equal(0.99, rows[2].Price);
    }

    [Fact]
    public async Task Rejects_Comma_When_Option_Is_Disabled()
    {
        string csv = 
            "id;price\r\n" +
            "1;12,5\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        CsvError? error = null;
        var options = new CsvReaderOptions
        {
            Delimiter = ';',
            AllowCommaDecimalSeparator = false,
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var rows = new List<PriceRecord>();
        await foreach (var row in FlashCsvSerializer.ReadAllAsync<PriceRecord>(stream, options))
        {
            rows.Add(row);
        }

        Assert.Empty(rows);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.InvalidFloat, error.Value.Code);
        Assert.Equal("12,5", error.Value.RawText);
    }
}
