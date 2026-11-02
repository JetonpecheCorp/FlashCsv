using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.SecuriteTest;

[CsvSerializable]
public partial record struct PlainArticleRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    // Aucune annotation locale [CsvSanitizeHtml] sur ces colonnes
    [CsvName("title")]
    public string Title { get; init; }

    [CsvName("summary")]
    public string Summary { get; init; }
}

public class GlobalHtmlSanitizerTests
{
    [Fact]
    public async Task Applies_Global_Sanitize_Mode_When_No_Attribute_Present()
    {
        string csv = 
            "id,title,summary\r\n" +
            "1,<b>Breaking News</b>,<script>malicious()</script>\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Configuration globale appliquée à tout le fichier
        var options = new CsvReaderOptions
        {
            DefaultHtmlSanitizeMode = HtmlSanitizeMode.Encode
        };

        var list = new List<PlainArticleRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<PlainArticleRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Single(list);
        // Toutes les chaînes non annotées ont bien été assainies automatiquement
        Assert.Equal("&lt;b&gt;Breaking News&lt;/b&gt;", list[0].Title);
        Assert.Equal("&lt;script&gt;malicious()&lt;/script&gt;", list[0].Summary);
    }

    [Fact]
    public async Task Leaves_Raw_Strings_When_DefaultHtmlSanitizeMode_Is_Null()
    {
        string rawHtml = "<h1>Titre</h1>";
        string csv = $"id,title,summary\r\n1,{rawHtml},test\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new CsvReaderOptions(); // DefaultHtmlSanitizeMode est null par défaut

        var list = new List<PlainArticleRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<PlainArticleRecord>(stream, options))
        {
            list.Add(row);
        }

        Assert.Single(list);
        Assert.Equal(rawHtml, list[0].Title);
    }
}
