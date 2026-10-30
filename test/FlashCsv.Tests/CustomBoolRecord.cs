using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct CustomBoolRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("is_active")]
    [CsvBoolean(["VRAI", "ACTIF", "1"], ["FAUX", "INACTIF", "0"])]
    public bool IsActive { get; init; }

    [CsvName("is_verified")]
    public bool IsVerified { get; init; } // Utilise les listes par défaut
}

// Modèle avec liste unique (repli implicite vers false)
[CsvSerializable]
public partial record struct FeatureRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("flag")]
    [CsvBoolean(["OUI", "1"])]
    public bool IsEnabled { get; init; }
}
