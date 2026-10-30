using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests;

public readonly struct CompanyVatCondition : ICsvCondition<OrderRecord>
{
    public static bool IsSatisfied(in OrderRecord entity) => entity.IsCompany;
}

[CsvSerializable]
public partial record struct OrderRecord
{
    [CsvName("order_id")]
    public int OrderId { get; init; }

    [CsvName("customer_email")]
    [CsvRequired] // Obligatoire même si le type est string?
    public string? CustomerEmail { get; init; }

    [CsvName("is_company")]
    public bool IsCompany { get; init; }

    [CsvName("vat_number")]
    [CsvRequiredIf<CompanyVatCondition>] // Obligatoire uniquement si IsCompany == true
    public string? VatNumber { get; init; }
}
