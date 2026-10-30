using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests.EnumTest;

public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered
}

[CsvSerializable]
public partial record struct OrderStatusRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("status_name")]
    [CsvEnum(CsvEnumFormat.Name)]
    public OrderStatus StatusByName { get; init; }

    [CsvName("status_code")]
    [CsvEnum(CsvEnumFormat.UnderlyingValue)]
    public OrderStatus StatusByCode { get; init; }

    [CsvName("status_opt")]
    public OrderStatus? StatusOptional { get; init; }
}
