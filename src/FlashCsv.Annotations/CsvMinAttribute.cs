namespace FlashCsv.Annotations;

/// <summary>
/// Valide qu'une valeur numérique est supérieure ou égale à une borne minimale.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvMinAttribute : Attribute
{
    public double ValueDouble { get; }
    public long ValueLong { get; }
    public bool IsInteger { get; }

    public CsvMinAttribute(int min)
    {
        ValueLong = min;
        ValueDouble = min;
        IsInteger = true;
    }

    public CsvMinAttribute(long min)
    {
        ValueLong = min;
        ValueDouble = min;
        IsInteger = true;
    }

    public CsvMinAttribute(double min)
    {
        ValueDouble = min;
        ValueLong = (long)min;
        IsInteger = false;
    }
}
