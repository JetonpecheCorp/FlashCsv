namespace FlashCsv.Annotations;

/// <summary>
/// Définit la colonne à utiliser pour déterminer le type polymorphe à instancier.
/// </summary>
/// <param name="columnName">Nom de l'en-tête CSV contenant le discriminant.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class CsvDiscriminatorAttribute(string columnName) : Attribute
{
    public string ColumnName => columnName;
}
