using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct NullValuesRecord
{
    public NullValuesRecord() { }

    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("description")]
    [CsvNullValues("VIDE", "AUCUN", "N/A")]
    public string? Description { get; init; }

    [CsvName("age")]
    [CsvNullValues("-1", "INCONNU")]
    public int? Age { get; init; }

    [CsvName("required_val")]
    [CsvNullValues("NON_RENSEIGNE")]
    public int RequiredVal { get; init; } // int non-nullable -> doit générer RequiredFieldMissing si présent
}
