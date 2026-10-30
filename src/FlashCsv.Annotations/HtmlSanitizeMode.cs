namespace FlashCsv.Annotations;

public enum HtmlSanitizeMode
{
    /// <summary>
    /// Convertit les caractères HTML critiques en entités inoffensives (&lt;, &gt;, &amp;, &quot;, &#39;).
    /// </summary>
    Encode = 0,

    /// <summary>
    /// Supprime intégralement les balises HTML (<...>).
    /// </summary>
    StripTags = 1
}
