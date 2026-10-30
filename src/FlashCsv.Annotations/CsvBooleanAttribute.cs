namespace FlashCsv.Annotations;

/// <summary>
/// Définit sur mesure les représentations textuelles acceptées comme valeurs booléennes pour une colonne spécifique.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class CsvBooleanAttribute: Attribute
{
    public string[] TrueValues { get; }
    public string[] FalseValues { get; }

    /// <summary>
    /// Spécifie uniquement les valeurs représentant true (tout le reste vaudra false).
    /// </summary>
    public CsvBooleanAttribute(string[] trueValues)
    {
        TrueValues = trueValues;
        FalseValues = [];
    }

    /// <summary>
    /// Spécifie à la fois les valeurs true et false.
    /// </summary>
    public CsvBooleanAttribute(string[] trueValues, string[] falseValues)
    {
        TrueValues = trueValues;
        FalseValues = falseValues;
    }
}
