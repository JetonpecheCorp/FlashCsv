using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.ArrayEscapeTest;

[CsvSerializable]
public partial record struct EscapedArrayRecord
{
    public EscapedArrayRecord() { }

    [CsvName("id")]
    public int Id { get; init; }

    // Délimiteur ';' et caractère d'échappement '\'
    [CsvName("tags")]
    [CsvDelimiterArray(';', '\\')]
    public string[] Tags { get; init; } = [];
}

[CsvSerializable]
public partial record struct ArrayAndAllowedRecord
{
    public ArrayAndAllowedRecord() { }

    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("currency")]
    [CsvAllowedValues("EUR", "USD", "GBP")]
    public string Currency { get; init; } = default!;

    [CsvName("tags")]
    [CsvDelimiterArray(';')]
    public string[] Tags { get; init; } = [];

    [CsvName("scores")]
    [CsvDelimiterArray('|')]
    public List<int> Scores { get; init; } = [];
}

public class EscapedArrayTests
{
    [Fact]
    public async Task Can_Deserialize_And_Serialize_With_Escaped_Delimiter()
    {
        // "c#\;net" contient un point-virgule échappé et ne doit pas être séparé en 2
        string csv = 
            "id,tags\r\n" +
            "1,dev;c#\\;net;backend\r\n";

        using var readStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var items = new List<EscapedArrayRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<EscapedArrayRecord>(readStream))
        {
            items.Add(row);
        }

        Assert.Single(items);
        Assert.Equal(3, items[0].Tags.Length);
        Assert.Equal("dev", items[0].Tags[0]);
        Assert.Equal("c#;net", items[0].Tags[1]); // Dé-échappé lors de la lecture
        Assert.Equal("backend", items[0].Tags[2]);

        // Roundtrip sérialisation (doit ré-échapper le ';')
        using var writeStream = new MemoryStream();
        await FlashCsvSerializer.SerializeAsync(writeStream, items);
        string outputCsv = System.Text.Encoding.UTF8.GetString(writeStream.ToArray());

        Assert.Contains("dev;c#\\;net;backend", outputCsv);
    }

    [Fact]
    public async Task Can_Deserialize_And_Serialize_Delimiter_Arrays()
    {
        string csv = 
            "id,currency,tags,scores\r\n" +
            "1,EUR,dev;dotnet;csharp,10|20|30\r\n";

        using var readStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var items = new List<ArrayAndAllowedRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<ArrayAndAllowedRecord>(readStream))
        {
            items.Add(row);
        }

        Assert.Single(items);
        Assert.Equal("EUR", items[0].Currency);
        Assert.Equal(["dev", "dotnet", "csharp"], items[0].Tags);
        Assert.Equal([10, 20, 30], items[0].Scores);

        // Roundtrip sérialisation
        using var writeStream = new MemoryStream();
        await FlashCsvSerializer.SerializeAsync(writeStream, items);
        string outputCsv = System.Text.Encoding.UTF8.GetString(writeStream.ToArray());

        Assert.Contains("dev;dotnet;csharp", outputCsv);
        Assert.Contains("10|20|30", outputCsv);
    }

    [Fact]
    public async Task Rejects_Value_Not_In_AllowedValues()
    {
        string csv = 
            "id,currency,tags,scores\r\n" +
            "1,JPY,dev,10\r\n"; // JPY n'est pas autorisé

        using var readStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        CsvError? capturedError = null;
        var options = new CsvReaderOptions
        {
            OnError = err => capturedError = err
        };

        var items = new List<ArrayAndAllowedRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<ArrayAndAllowedRecord>(readStream, options))
        {
            items.Add(row);
        }

        Assert.Empty(items);
        Assert.NotNull(capturedError);
        Assert.Equal(CsvErrorCode.InvalidValue, capturedError.Value.Code);
        Assert.Equal("Currency", capturedError.Value.ColumnName);
    }
}
