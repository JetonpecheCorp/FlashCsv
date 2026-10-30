namespace FlashCsv.Annotations;

/// <summary>
/// Spécifie le nom de sérialisation et les alias de désérialisation pour une valeur d'énumération.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class CsvEnumMemberNameAttribute(string name, params string[] aliases) : Attribute
{
    /// <summary>
    /// Nom principal utilisé pour la sérialisation.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Alias alternatifs acceptés lors de la désérialisation.
    /// </summary>
    public string[] Aliases { get; } = aliases ?? [];
}
