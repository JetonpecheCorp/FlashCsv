using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.FormulaTest;

[CsvSerializable]
public partial record struct AddressRecord
{
    [CsvName("street")]
    public string Street { get; init; }

    [CsvName("city")]
    public string City { get; init; }
}

[CsvSerializable]
public partial record struct CustomerOrderRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvFlatten(Prefix = "shipping_")]
    public AddressRecord ShippingAddress { get; init; }

    [CsvName("formula_text")]
    [CsvSanitizeFormula]
    public string FormulaText { get; init; }
}

public class FormulaAndFlattenTests
{
    [Fact]
    public void GetHeaderNames_Flattens_SubObject_Headers()
    {
        string[] headers = CustomerOrderRecord.GetHeaderNames();

        Assert.Equal(4, headers.Length);
        Assert.Equal("id", headers[0]);
        Assert.Equal("shipping_street", headers[1]);
        Assert.Equal("shipping_city", headers[2]);
        Assert.Equal("formula_text", headers[3]);
    }

    [Fact]
    public async Task Serializes_And_Escapes_Formula()
    {
        var records = new[]
        {
            new CustomerOrderRecord
            {
                Id = 1,
                ShippingAddress = new AddressRecord { Street = "10 rue de la Paix", City = "Paris" },
                FormulaText = "=SUM(A1:B1)" // Débute par '=' -> doit être préfixé par une apostrophe
            }
        };

        using var stream = new MemoryStream();
        await FlashCsvSerializer.SerializeAsync(stream, records);

        string csv = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Contains("shipping_street,shipping_city", csv);
        Assert.Contains("10 rue de la Paix,Paris", csv);
        Assert.Contains("'=SUM(A1:B1)", csv);
    }

    [Fact]
    public async Task Deserializes_Flattened_Record()
    {
        string csv = 
            "id,shipping_street,shipping_city,formula_text\r\n" +
            "100,5th Avenue,New York,normal_text\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<CustomerOrderRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<CustomerOrderRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Single(list);
        Assert.Equal(100, list[0].Id);
        Assert.Equal("5th Avenue", list[0].ShippingAddress.Street);
        Assert.Equal("New York", list[0].ShippingAddress.City);
        Assert.Equal("normal_text", list[0].FormulaText);
    }
}
