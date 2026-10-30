namespace FlashCsv.Annotations;

/// <summary>
/// Nettoie ou neutralise le contenu HTML pour prévenir les injections et failles XSS.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvSanitizeHtmlAttribute(HtmlSanitizeMode mode = HtmlSanitizeMode.Encode) : Attribute
{
    public HtmlSanitizeMode Mode => mode;
}
