using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.ModeStrictTest;

[CsvSerializable]
public partial record struct StrictUserRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("name")]
    public string Name { get; init; }
}

public class ExtraColumnsTests
{
    [Fact]
    public async Task Ignores_Extra_Columns_By_Default()
    {
        // 4 colonnes dans le CSV alors que le modèle n'en a que 2
        string csv = 
            "id,name,extra1,extra2\r\n" +
            "1,Alice,ignore_me,me_too\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<StrictUserRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<StrictUserRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Alice", list[0].Name);
    }

    [Fact]
    public async Task Fails_When_IgnoreExtraColumns_Is_False()
    {
        // La ligne 1 a 3 colonnes au lieu des 2 prévues par l'en-tête
        string csv = 
            "id,name\r\n" +
            "1,Alice,unexpected_column\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            IgnoreExtraColumns = false, // Mode strict activé
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<StrictUserRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<StrictUserRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.TooManyColumns, error.Value.Code);
        Assert.Equal("unexpected_column", error.Value.RawText);
    }
}
