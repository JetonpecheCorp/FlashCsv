using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.StringTest;

[CsvSerializable]
public partial record struct ContactRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("postal_code")]
    [CsvPattern(@"^\d{5}$")] // 5 chiffres obligatoires
    public string PostalCode { get; init; }

    [CsvName("email")]
    [CsvPattern(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    public string Email { get; init; }
}

public class RegexTest
{
    [Fact]
    public async Task Accepts_Matching_Patterns()
    {
        string csv = 
            "id,postal_code,email\r\n" +
            "1,69150,contact@flashcsv.dev\r\n" +
            "2,75001,test@example.com\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<ContactRecord>();

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<ContactRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Equal(2, list.Count);
        Assert.Equal("69150", list[0].PostalCode);
        Assert.Equal("contact@flashcsv.dev", list[0].Email);
    }

    [Fact]
    public async Task Rejects_Invalid_Postal_Code_Pattern()
    {
        string csv = 
            "id,postal_code,email\r\n" +
            "1,69A50,contact@flashcsv.dev\r\n"; // Contient une lettre

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<ContactRecord>();
        await foreach (var row in FlashCsvSerializer.ReadAllAsync<ContactRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.PatternMismatch, error.Value.Code);
        Assert.Equal("PostalCode", error.Value.ColumnName);
        Assert.Equal("69A50", error.Value.RawText);
    }

    [Fact]
    public async Task Rejects_Invalid_Email_Pattern()
    {
        string csv = 
            "id,postal_code,email\r\n" +
            "1,69150,not-an-email\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<ContactRecord>();
        await foreach (var row in FlashCsvSerializer.ReadAllAsync<ContactRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.PatternMismatch, error.Value.Code);
        Assert.Equal("Email", error.Value.ColumnName);
    }
}
