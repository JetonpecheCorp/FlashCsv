namespace FlashCsv.Annotations;

/// <summary>
/// Valide qu'une valeur numérique se situe entre des bornes minimale et maximale incluses.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvRangeAttribute : Attribute
{
    public double MinDouble { get; }
    public double MaxDouble { get; }
    public long MinLong { get; }
    public long MaxLong { get; }
    public bool IsIntegerRange { get; }

    public CsvRangeAttribute(int min, int max)
    {
        MinLong = min;
        MaxLong = max;
        MinDouble = min;
        MaxDouble = max;
        IsIntegerRange = true;
    }

    public CsvRangeAttribute(long min, long max)
    {
        MinLong = min;
        MaxLong = max;
        MinDouble = min;
        MaxDouble = max;
        IsIntegerRange = true;
    }

    public CsvRangeAttribute(double min, double max)
    {
        MinDouble = min;
        MaxDouble = max;
        MinLong = (long)min;
        MaxLong = (long)max;
        IsIntegerRange = false;
    }
}
