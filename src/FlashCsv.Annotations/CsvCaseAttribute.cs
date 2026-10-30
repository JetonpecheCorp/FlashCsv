namespace FlashCsv.Annotations;

public enum CsvCaseMode
{
    Upper = 0,
    Lower = 1,
    Title = 2
}

/// <summary>
/// Normalise automatiquement la casse du texte lors de la désérialisation ou de la sérialisation.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvCaseAttribute(CsvCaseMode mode) : Attribute
{
    public CsvCaseMode Mode => mode;
}
