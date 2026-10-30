using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests;

public class RequiredIfAttrTest
{
    [Fact]
    public async Task Rejects_When_CsvRequired_Field_Is_Empty()
    {
        // customer_email est vide
        string csv = 
            "order_id,customer_email,is_company,vat_number\r\n" +
            "101,,false,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        CsvError? captured = null;
        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => captured = err
        };

        var list = new List<OrderRecord>();
        await foreach (var item in FlashCsvSerializer.ReadAllAsync<OrderRecord>(stream, options))
        {
            list.Add(item);
        }

        Assert.Empty(list);
        Assert.NotNull(captured);
        Assert.Equal(2, captured.Value.LineNumber);
        Assert.Equal("CustomerEmail", captured.Value.ColumnName);
        Assert.Equal(CsvErrorCode.RequiredFieldMissing, captured.Value.Code);
    }

    [Fact]
    public async Task Allows_Empty_When_Condition_Is_False()
    {
        // is_company = false -> vat_number vide est accepté
        string csv = 
            "order_id,customer_email,is_company,vat_number\r\n" +
            "101,particulier@test.com,false,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var list = new List<OrderRecord>();
        await foreach (var item in FlashCsvSerializer.ReadAllAsync<OrderRecord>(stream))
        {
            list.Add(item);
        }

        Assert.Single(list);
        Assert.Equal("particulier@test.com", list[0].CustomerEmail);
        Assert.False(list[0].IsCompany);
        Assert.Null(list[0].VatNumber);
    }

    [Fact]
    public async Task Rejects_When_Condition_Is_True_And_Field_Is_Missing()
    {
        // is_company = true -> vat_number est vide alors qu'il est requis par CompanyVatCondition
        string csv = 
            "order_id,customer_email,is_company,vat_number\r\n" +
            "102,pro@test.com,true,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        CsvError? captured = null;
        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => captured = err
        };

        var list = new List<OrderRecord>();
        await foreach (var item in FlashCsvSerializer.ReadAllAsync<OrderRecord>(stream, options))
        {
            list.Add(item);
        }

        Assert.Empty(list);
        Assert.NotNull(captured);
        Assert.Equal(2, captured.Value.LineNumber);
        Assert.Equal("VatNumber", captured.Value.ColumnName);
        Assert.Equal(CsvErrorCode.RequiredFieldMissing, captured.Value.Code);
    }

    [Fact]
    public async Task Accepts_When_Condition_Is_True_And_Field_Is_Supplied()
    {
        // is_company = true et vat_number est renseigné
        string csv = 
            "order_id,customer_email,is_company,vat_number\r\n" +
            "103,pro@test.com,true,FR99887766554\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var list = new List<OrderRecord>();
        await foreach (var item in FlashCsvSerializer.ReadAllAsync<OrderRecord>(stream))
        {
            list.Add(item);
        }

        Assert.Single(list);
        Assert.True(list[0].IsCompany);
        Assert.Equal("FR99887766554", list[0].VatNumber);
    }
}
