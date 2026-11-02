using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.ValeurDefautTest;

[CsvSerializable]
public partial record struct DefaultUserRecord
{
    // Constructeur sans paramètre requis par C# en présence d'initialiseurs
    public DefaultUserRecord() { }

    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("email")]
    [CsvCase(CsvCaseMode.Lower)]
    public string Email { get; init; }

    [CsvName("country")]
    [CsvCase(CsvCaseMode.Upper)]
    public string Country { get; init; }

    [CsvName("role")]
    public string Role { get; init; } = "standard_user";

    [CsvName("points")]
    public int Points { get; init; } = 100;
}

public class CaseAndDefaultsTests
{
    [Fact]
    public async Task Preserves_Default_Initializers_When_Cells_Are_Null()
    {
        // Les colonnes role et points sont laissées vides
        string csv = 
            "id,email,country,role,points\r\n" +
            "1,User@Domain.COM,fr,,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<DefaultUserRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<DefaultUserRecord>(stream))
        {
            list.Add(row);
        }

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        // Normalisation CsvCase
        Assert.Equal("user@domain.com", list[0].Email);
        Assert.Equal("FR", list[0].Country);
        // Conservation des valeurs par défaut C# !
        Assert.Equal("standard_user", list[0].Role);
        Assert.Equal(100, list[0].Points);
    }

    [Fact]
    public async Task TransformField_Applies_Globally_On_Reader_And_Writer()
    {
        var records = new[]
        {
            new DefaultUserRecord { Id = 1, Email = "test@abc.com", Country = "FR" }
        };

        // 1. Écriture avec TransformField (ex: préfixer l'email)
        using var stream = new MemoryStream();
        var writerOptions = new CsvWriterOptions
        {
            TransformField = (col, val) => col == "Email" ? "mailto:" + val : val
        };

        await FlashCsvSerializer.SerializeAsync(stream, records, writerOptions);
        string csv = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("mailto:test@abc.com", csv);

        // 2. Lecture avec TransformField (ex: suppression du préfixe)
        using var readStream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var readerOptions = new CsvReaderOptions
        {
            TransformField = (col, val) => col == "Email" && val.StartsWith("mailto:") ? val["mailto:".Length..] : val
        };

        var list = new List<DefaultUserRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<DefaultUserRecord>(readStream, readerOptions))
        {
            list.Add(row);
        }

        Assert.Single(list);
        Assert.Equal("test@abc.com", list[0].Email);
    }
}
