using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.SecuriteTest;

[CsvSerializable]
public partial record struct FeedbackRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("comment")]
    [CsvSanitizeHtml(HtmlSanitizeMode.Encode)]
    public string Comment { get; init; }

    [CsvName("plain_text")]
    [CsvSanitizeHtml(HtmlSanitizeMode.StripTags)]
    public string PlainText { get; init; }

    [CsvName("raw_content")]
    public string RawContent { get; init; }
}

public class HtmlSanitizerTests
{
    [Fact]
    public async Task Sanitizes_Html_Properly()
    {
        string maliciousPayload = "<script>alert('xss')</script><b>Hello</b>";
        string csv = $"id,comment,plain_text,raw_content\r\n1,\"{maliciousPayload}\",\"{maliciousPayload}\",\"{maliciousPayload}\"\r\n";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<FeedbackRecord>();

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<FeedbackRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Single(list);

        // 1. Encode : les balises sont neutralisées (les guillemets CSV d'encapsulation ont été proprement retirés par le lecteur)
        Assert.Equal("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;&lt;b&gt;Hello&lt;/b&gt;", list[0].Comment);

        // 2. StripTags : les balises sont retirées
        Assert.Equal("alert('xss')Hello", list[0].PlainText);

        // 3. Sans attribut : reste brut
        Assert.Equal(maliciousPayload, list[0].RawContent);
    }
}