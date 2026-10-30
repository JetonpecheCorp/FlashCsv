using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests.IgnoreTest;

[CsvSerializable]
public partial record struct AccountRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("is_admin")]
    public bool IsAdmin { get; init; }

    // Toujours ignoré (propriété locale)
    [CsvIgnore]
    public string? LocalCache { get; init; }

    // Ignoré si IsAdmin == false
    [CsvName("internal_notes")]
    [CsvIgnoreIf<SkipInternalNotesCondition>]
    public string? InternalNotes { get; init; }
}

public readonly struct SkipInternalNotesCondition : ICsvCondition<AccountRecord>
{
    // Ignore les notes internes si l'utilisateur n'est pas un administrateur
    public static bool IsSatisfied(in AccountRecord entity) => !entity.IsAdmin;
}
