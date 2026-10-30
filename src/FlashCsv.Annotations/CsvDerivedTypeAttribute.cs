namespace FlashCsv.Annotations;

/// <summary>
/// Associe une valeur de discriminant à un type dérivé spécifique pour le polymorphisme.
/// </summary>
/// <param name="targetType">Le type enfant à instancier.</param>
/// <param name="value">La valeur attendue dans la colonne discriminante pour ce type.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public sealed class CsvDerivedTypeAttribute(Type targetType, string value) : Attribute
{
    public Type TargetType => targetType;
    public string Value => value;
}
