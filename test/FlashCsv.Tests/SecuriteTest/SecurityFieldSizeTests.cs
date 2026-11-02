using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.SecuriteTest;

[CsvSerializable]
public partial record struct DocumentRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("content")]
    public string Content { get; init; }
}
public class SecurityFieldSizeTests
{
    [Fact]
    public async Task Fails_When_Field_Exceeds_MaxFieldSize()
    {
        // Une cellule content de 100 octets
        string largeContent = new string('A', 100);
        string csv = $"id,content\r\n1,{largeContent}\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            MaxFieldSize = 50, // Limite stricte à 50 octets
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<DocumentRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<DocumentRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.FieldTooLarge, error.Value.Code);
        Assert.Equal("Content", error.Value.ColumnName);
    }
}
