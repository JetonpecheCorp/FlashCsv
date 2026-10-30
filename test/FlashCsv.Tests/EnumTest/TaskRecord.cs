using FlashCsv.Annotations;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests.EnumTest;

public enum TaskPriority
{
    [CsvEnumMemberName("faible", "low", "1")]
    Low,

    [CsvEnumMemberName("moyen", "medium", "2")]
    Medium,

    [CsvEnumMemberName("eleve", "high", "urgent", "3")]
    High
}

[CsvSerializable]
public partial record struct TaskRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("priority")]
    public TaskPriority Priority { get; init; }

    [CsvName("opt_priority")]
    public TaskPriority? OptionalPriority { get; init; }
}
