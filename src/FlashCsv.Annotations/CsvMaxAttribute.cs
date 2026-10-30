namespace FlashCsv.Annotations;

/// <summary>
/// Valide qu'une valeur numérique est inférieure ou égale à une borne maximale.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvMaxAttribute : Attribute
{
    public double ValueDouble { get; }
    public long ValueLong { get; }
    public bool IsInteger { get; }

    public CsvMaxAttribute(int max)
    {
        ValueLong = max;
        ValueDouble = max;
        IsInteger = true;
    }

    public CsvMaxAttribute(long max)
    {
        ValueLong = max;
        ValueDouble = max;
        IsInteger = true;
    }

    public CsvMaxAttribute(double max)
    {
        ValueDouble = max;
        ValueLong = (long)max;
        IsInteger = false;
    }
}