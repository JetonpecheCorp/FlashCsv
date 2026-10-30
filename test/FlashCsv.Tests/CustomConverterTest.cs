using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests;

public class CustomConverterTest
{
    [Fact]
    public async Task Can_Read_And_Write_Using_Custom_Field_Converters()
    {
        // 1. Données CSV en entrée
        string csvInput = 
            "sku,price\r\n" +
            "PROD-001,49.99EUR\r\n" +
            "PROD-002,120.00USD\r\n";

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(csvInput));

        // 2. Lecture et désérialisation via ICsvFieldReader
        var products = new List<ProductRecord>();
        await foreach (var item in FlashCsvSerializer.ReadAllAsync<ProductRecord>(inputStream))
        {
            products.Add(item);
        }

        Assert.Equal(2, products.Count);

        Assert.Equal("PROD-001", products[0].Sku);
        Assert.Equal(49.99m, products[0].Price.Amount);
        Assert.Equal("EUR", products[0].Price.Currency);

        Assert.Equal("PROD-002", products[1].Sku);
        Assert.Equal(120.00m, products[1].Price.Amount);
        Assert.Equal("USD", products[1].Price.Currency);

        // 3. Sérialisation via ICsvFieldWriter
        using var outputStream = new MemoryStream();
        await FlashCsvSerializer.SerializeAsync(outputStream, products);

        string csvOutput = Encoding.UTF8.GetString(outputStream.ToArray());

        // 4. Vérification de la sortie formatée
        Assert.Contains("sku,price", csvOutput);
        Assert.Contains("PROD-001,49.99EUR", csvOutput);
        Assert.Contains("PROD-002,120.00USD", csvOutput);
    }

    [Fact]
    public async Task Reports_Converter_ErrorCode_When_Custom_Format_Is_Invalid()
    {
        // "abcEUR" au lieu d'un montant décimal
        string csvInput = 
            "sku,price\r\n" +
            "PROD-001,abcEUR\r\n";

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(csvInput));

        CsvError? capturedError = null;
        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => capturedError = err
        };

        var list = new List<ProductRecord>();
        await foreach (var item in FlashCsvSerializer.ReadAllAsync<ProductRecord>(inputStream, options))
        {
            list.Add(item);
        }

        Assert.Empty(list);
        Assert.NotNull(capturedError);
        Assert.Equal(2, capturedError.Value.LineNumber);
        Assert.Equal("Price", capturedError.Value.ColumnName);
        Assert.Equal(CsvErrorCode.InvalidDecimal, capturedError.Value.Code);
        Assert.Equal("abcEUR", capturedError.Value.RawText);
    }
}
