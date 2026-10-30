using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.EnumTest;

public class EnumAttrTest
{
    [Fact]
    public async Task Can_Deserialize_Enums_By_Name_And_Value()
    {
        string csv = 
            "id,status_name,status_code,status_opt\r\n" +
            "1,Shipped,2,Delivered\r\n" +
            "2,Pending,0,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<OrderStatusRecord>();

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<OrderStatusRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Equal(2, list.Count);

        // Ligne 1
        Assert.Equal(1, list[0].Id);
        Assert.Equal(OrderStatus.Shipped, list[0].StatusByName);
        Assert.Equal(OrderStatus.Delivered, list[0].StatusByCode);
        Assert.Equal(OrderStatus.Delivered, list[0].StatusOptional);

        // Ligne 2
        Assert.Equal(2, list[1].Id);
        Assert.Equal(OrderStatus.Pending, list[1].StatusByName);
        Assert.Equal(OrderStatus.Pending, list[1].StatusByCode);
        Assert.Null(list[1].StatusOptional);
    }

    [Fact]
    public async Task Rejects_Invalid_Enum_Value()
    {
        string csv = 
            "id,status_name,status_code,status_opt\r\n" +
            "1,UnknownStatus,0,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var list = new List<OrderStatusRecord>();
        await foreach (var row in FlashCsvSerializer.ReadAllAsync<OrderStatusRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Empty(list);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.InvalidEnum, error.Value.Code);
        Assert.Equal("StatusByName", error.Value.ColumnName);
    }
}
