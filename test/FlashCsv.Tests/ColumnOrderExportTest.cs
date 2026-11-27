using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;
using Xunit;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct ExportOrderRecord
{
    [CsvName("id_client", "id")]
    public int Id { get; init; }

    [CsvName("nom")]
    public string Name { get; init; }

    [CsvName("email")]
    public string Email { get; init; }

    [CsvName("solde")]
    public decimal Balance { get; init; }
}

public partial record struct ExportAddress
{
    [CsvName("rue")]
    public string Street { get; init; }

    [CsvName("ville")]
    public string City { get; init; }
}

[CsvSerializable]
public partial record struct ExportFlattenedOrderRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvFlatten("livraison_")]
    public ExportAddress Shipping { get; init; }
}

public class ColumnOrderExportTest
{
    [Fact]
    public void Serialize_WithColumnOrderByPropertyName_ShouldReorderHeaderAndValues()
    {
        var items = new[]
        {
            new ExportOrderRecord { Id = 1, Name = "Alice", Email = "alice@test.com", Balance = 125.50m },
            new ExportOrderRecord { Id = 2, Name = "Bob", Email = "bob@test.com", Balance = 300m }
        };

        var options = new CsvWriterOptions
        {
            ColumnOrder = ["Email", "Name", "Balance", "Id"]
        };

        string csv = FlashCsvSerializer.SerializeToString(items, options);
        string[] lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.Equal("email,nom,solde,id_client", lines[0]);
        Assert.Equal("alice@test.com,Alice,125.50,1", lines[1]);
        Assert.Equal("bob@test.com,Bob,300,2", lines[2]);
    }

    [Fact]
    public void Serialize_WithColumnOrderByCsvAliasCaseInsensitive_ShouldWork()
    {
        var items = new[]
        {
            new ExportOrderRecord { Id = 42, Name = "Charlie", Email = "c@test.com", Balance = 99.99m }
        };

        var options = new CsvWriterOptions
        {
            ColumnOrder = ["SOLDE", "id_client", "NOM"]
        };

        string csv = FlashCsvSerializer.SerializeToString(items, options);
        string[] lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
        Assert.Equal("solde,id_client,nom", lines[0]);
        Assert.Equal("99.99,42,Charlie", lines[1]);
    }

    [Fact]
    public void Serialize_WithSubsetAndUnknownColumns_ShouldExportOnlyValidSelectedColumns()
    {
        var items = new[]
        {
            new ExportOrderRecord { Id = 7, Name = "Diana", Email = "d@test.com", Balance = 50m }
        };

        var options = new CsvWriterOptions
        {
            ColumnOrder = ["Name", "NonExistentCol", "Id"]
        };

        string csv = FlashCsvSerializer.SerializeToString(items, options);
        string[] lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
        Assert.Equal("nom,id_client", lines[0]);
        Assert.Equal("Diana,7", lines[1]);
    }

    [Fact]
    public void Serialize_WithFlattenedColumnsAndFormatHeader_ShouldReorderAndFormat()
    {
        var items = new[]
        {
            new ExportFlattenedOrderRecord
            {
                Id = 10,
                Shipping = new ExportAddress { Street = "10 Rue de Paris", City = "Lyon" }
            }
        };

        var options = new CsvWriterOptions
        {
            ColumnOrder = ["livraison_ville", "id", "livraison_rue"],
            FormatHeader = h => h.ToUpperInvariant()
        };

        string csv = FlashCsvSerializer.SerializeToString(items, options);
        string[] lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
        Assert.Equal("LIVRAISON_VILLE,ID,LIVRAISON_RUE", lines[0]);
        Assert.Equal("Lyon,10,10 Rue de Paris", lines[1]);
    }

    [Fact]
    public async Task SerializeAsync_IAsyncEnumerable_WithColumnOrder_ShouldStreamOrderedColumns()
    {
        static async IAsyncEnumerable<ExportOrderRecord> GetItemsAsync()
        {
            yield return new ExportOrderRecord { Id = 1, Name = "Alice", Email = "a@test.com", Balance = 10m };
            await Task.Yield();
            yield return new ExportOrderRecord { Id = 2, Name = "Bob", Email = "b@test.com", Balance = 20m };
        }

        var options = new CsvWriterOptions
        {
            ColumnOrder = ["Balance", "Name"]
        };

        using var ms = new MemoryStream();
        await FlashCsvSerializer.SerializeAsync(ms, GetItemsAsync(), options, leaveOpen: true);

        string csv = Encoding.UTF8.GetString(ms.ToArray());
        string[] lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.Equal("solde,nom", lines[0]);
        Assert.Equal("10,Alice", lines[1]);
        Assert.Equal("20,Bob", lines[2]);
    }
}
