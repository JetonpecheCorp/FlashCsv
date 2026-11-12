using System.Data.Common;
using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;
using Xunit;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct DataReaderItem
{
    public int Id { get; init; }
    public string Name { get; init; }
    public double Score { get; init; }
}

public class DataReaderTest
{
    [Fact]
    public async Task CreateDataReader_FromStream_ShouldReadRowsCorrectly()
    {
        string csv = """
Id,Name,Score
1,Alpha,95.5
2,Beta,88.0
""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        using DbDataReader reader = FlashCsvSerializer.CreateDataReader<DataReaderItem>(stream);

        Assert.Equal(3, reader.FieldCount);
        Assert.Equal("Id", reader.GetName(0));
        Assert.Equal("Name", reader.GetName(1));
        Assert.Equal("Score", reader.GetName(2));

        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("Alpha", reader.GetString(1));
        Assert.Equal(95.5, reader.GetDouble(2));

        Assert.True(await reader.ReadAsync());
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("Beta", reader.GetString(1));
        Assert.Equal(88.0, reader.GetDouble(2));

        Assert.False(await reader.ReadAsync());
    }

    [Fact]
    public async Task CreateDataReader_FromEnumerable_ShouldReadRowsCorrectly()
    {
        var items = new[]
        {
            new DataReaderItem { Id = 10, Name = "Item10", Score = 100 },
            new DataReaderItem { Id = 20, Name = "Item20", Score = 200 }
        };

        using DbDataReader reader = FlashCsvSerializer.CreateDataReader(items);

        Assert.True(await reader.ReadAsync());
        Assert.Equal(10, reader.GetInt32(0));
        Assert.Equal("Item10", reader.GetString(1));

        Assert.True(await reader.ReadAsync());
        Assert.Equal(20, reader.GetInt32(0));
        Assert.Equal("Item20", reader.GetString(1));

        Assert.False(await reader.ReadAsync());
    }
}
