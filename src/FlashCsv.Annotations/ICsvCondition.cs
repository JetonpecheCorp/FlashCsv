namespace FlashCsv.Annotations;

/// <summary>
/// Contrat abstrait statique pour évaluer une condition de validation zéro-allocation.
/// </summary>
/// <typeparam name="T">Type du modèle en cours de désérialisation.</typeparam>
public interface ICsvCondition<T>
{
    /// <summary>
    /// Évalue si la condition rendant un champ obligatoire est satisfaite.
    /// </summary>
    /// <param name="entity">Instance partiellement désérialisée avec les champs déjà lus.</param>
    /// <returns><see langword="true"/> si la condition est remplie ; sinon <see langword="false"/>.</returns>
    static abstract bool IsSatisfied(in T entity);
}
