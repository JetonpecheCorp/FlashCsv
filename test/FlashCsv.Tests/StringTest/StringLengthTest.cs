using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.StringTest;

[CsvSerializable]
public partial record struct CustomerRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("code")]
    [CsvLength(3, 5)] // Entre 3 et 5 caractères
    public string Code { get; init; }

    [CsvName("username")]
    [CsvMinLength(3)] // Au moins 3 caractères
    public string Username { get; init; }

    [CsvName("bio")]
    [CsvMaxLength(10)] // Au plus 10 caractères
    public string? Bio { get; init; }
}

public class StringLengthTest
{
    [Fact]
    public async Task Accepts_Strings_With_Valid_Lengths()
    {
        string csv = 
            "id,code,username,bio\r\n" +
            "1,ABC,john,court\r\n" +
            "2,ABCDE,alice,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<CustomerRecord>();

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<CustomerRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Equal(2, list.Count);
        Assert.Equal("ABC", list[0].Code);
        Assert.Equal("john", list[0].Username);
        Assert.Equal("court", list[0].Bio);

        Assert.Equal("ABCDE", list[1].Code);
        Assert.Equal("alice", list[1].Username);
        Assert.Null(list[1].Bio);
    }

    [Fact]
    public async Task Rejects_Too_Short_String()
    {
        string csv = 
            "id,code,username,bio\r\n" +
            "1,AB,john,court\r\n"; // Code fait 2 caractères (< 3)

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<CustomerRecord>();
        await foreach (var row in FlashCsvSerializer.ReadAllAsync<CustomerRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.InvalidLength, error.Value.Code);
        Assert.Equal("Code", error.Value.ColumnName);
    }

    [Fact]
    public async Task Rejects_Too_Long_String()
    {
        string csv = 
            "id,code,username,bio\r\n" +
            "1,ABC,john,un commentaire beaucoup trop long\r\n"; // Bio dépasse 10 caractères

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<CustomerRecord>();
        await foreach (var row in FlashCsvSerializer.ReadAllAsync<CustomerRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.InvalidLength, error.Value.Code);
        Assert.Equal("Bio", error.Value.ColumnName);
    }
}
