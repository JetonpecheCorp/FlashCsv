using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct UserRecord
{
    [CsvName("identifiant", "id_user")]
    public int Id { get; init; }

    [CsvName("nom_complet")]
    public string Name { get; init; }

    public double Score { get; init; }

    [CsvName("date_creation")]
    public DateTime CreatedAt { get; init; }

    [CsvJson]
    public string[] Roles { get; init; }
}
