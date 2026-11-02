using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.OrderTest;

[CsvSerializable]
public partial record struct OrderedRecord
{
    // Dans le code C#, 'Third' est déclaré en premier, mais son Order vaut 3
    [CsvOrder(3)]
    [CsvName("troisieme")]
    public string Third { get; init; }

    // 'First' est déclaré en deuxième, mais son Order vaut 1
    [CsvOrder(1)]
    [CsvName("premier")]
    public int First { get; init; }

    // 'Second' a Order = 2
    [CsvOrder(2)]
    [CsvName("deuxieme")]
    public string Second { get; init; }
}

public class CsvOrderTests
{
    [Fact]
    public void GetHeaderNames_Respects_CsvOrder()
    {
        string[] headers = OrderedRecord.GetHeaderNames();

        Assert.Equal(3, headers.Length);
        Assert.Equal("premier", headers[0]);
        Assert.Equal("deuxieme", headers[1]);
        Assert.Equal("troisieme", headers[2]);
    }

    [Fact]
    public async Task Serializes_In_Specified_Order()
    {
        var records = new[]
        {
            new OrderedRecord { First = 10, Second = "milieu", Third = "fin" }
        };

        using var stream = new MemoryStream();
        await FlashCsvSerializer.SerializeAsync(stream, records);

        string csv = Encoding.UTF8.GetString(stream.ToArray());

        // L'en-tête et les valeurs doivent être ordonnés : premier, deuxieme, troisieme
        Assert.StartsWith("premier,deuxieme,troisieme\r\n10,milieu,fin", csv);
    }

    [Fact]
    public async Task Deserializes_Without_Headers_In_Specified_Order()
    {
        // En mode sans en-tête (HasHeaderRecord = false), la position physique 0 doit correspondre à Order(1)
        string csv = "42,texte,dernier\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new CsvReaderOptions { HasHeader = false };

        var list = new List<OrderedRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<OrderedRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Single(list);
        Assert.Equal(42, list[0].First);
        Assert.Equal("texte", list[0].Second);
        Assert.Equal("dernier", list[0].Third);
    }
}
