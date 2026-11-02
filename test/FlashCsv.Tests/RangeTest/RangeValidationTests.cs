using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.RangeTest;

[CsvSerializable]
public partial record struct UserAgeRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("age")]
    [CsvRange(0, 120)]
    public int Age { get; init; }

    [CsvName("score")]
    [CsvRange(0.0, 100.0)]
    public double Score { get; init; }

    [CsvName("bonus")]
    [CsvRange(10, 50)]
    public int? OptionalBonus { get; init; }
}

public class RangeValidationTests
{
    [Fact]
    public async Task Accepts_Values_Within_Range()
    {
        string csv = 
            "id,age,score,bonus\r\n" +
            "1,25,88.5,15\r\n" +
            "2,0,0.0,\r\n" +
            "3,120,100.0,50\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<UserAgeRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<UserAgeRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Equal(3, list.Count);
        Assert.Equal(25, list[0].Age);
        Assert.Equal(88.5, list[0].Score);
        Assert.Equal(15, list[0].OptionalBonus);

        Assert.Equal(0, list[1].Age);
        Assert.Null(list[1].OptionalBonus);
    }

    [Fact]
    public async Task Rejects_Values_Outside_Range()
    {
        string csv = 
            "id,age,score,bonus\r\n" +
            "1,150,50.0,\r\n"; // 150 dépasse la borne max 120

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<UserAgeRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<UserAgeRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.ValueOutOfRange, error.Value.Code);
        Assert.Equal("Age", error.Value.ColumnName);
        Assert.Equal("150", error.Value.RawText);
    }

    [Fact]
    public async Task Rejects_Nullable_Value_When_Present_And_Outside_Range()
    {
        string csv = 
            "id,age,score,bonus\r\n" +
            "1,30,50.0,5"; // 5 est inférieur à la borne min 10 pour bonus

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<UserAgeRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<UserAgeRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.ValueOutOfRange, error.Value.Code);
        Assert.Equal("OptionalBonus", error.Value.ColumnName);
        Assert.Equal("5", error.Value.RawText);
    }
}
