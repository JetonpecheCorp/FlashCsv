using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.FonctionAvancerTest;

[CsvSerializable]
public partial record struct AutoAliasAndRowRecord
{
    [CsvRowNumber]
    public int Line { get; init; } // Sera injecté automatiquement

    public int Id { get; init; }

    // Pas de [CsvName] ! Le générateur doit gérer "first_name", "first-name", "firstName"
    public string FirstName { get; init; }

    // Pas de [CsvName] ! Doit gérer "date_of_birth", "date-of-birth", etc.
    public string DateOfBirth { get; init; }
}

[CsvSerializable]
public partial record struct ExtensionDataRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    [CsvName("name")]
    public string Name { get; init; }

    [CsvExtensionData]
    public Dictionary<string, string> ExtraColumns { get; init; } // Capturera le reste
}

public class AdvancedFeaturesTests
{
    [Fact]
    public async Task Can_Map_Columns_Automatically_And_Inject_RowNumber()
    {
        // En-têtes : snake_case ("first_name") et kebab-case ("date-of-birth")
        string csv = 
            "id,first_name,date-of-birth\r\n" +
            "1,Jean,1990-01-01\r\n" +
            "2,Marie,1985-05-05\r\n";

        using var readStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var items = new List<AutoAliasAndRowRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<AutoAliasAndRowRecord>(readStream))
        {
            items.Add(row);
        }

        Assert.Equal(2, items.Count);

        // Vérification de l'injection du numéro de ligne (Ligne 1 = en-tête, Ligne 2 = première donnée)
        Assert.Equal(2, items[0].Line);
        Assert.Equal(3, items[1].Line);

        // Vérification de l'auto-aliasing (PascalCase vers snake_case / kebab-case)
        Assert.Equal(1, items[0].Id);
        Assert.Equal("Jean", items[0].FirstName);
        Assert.Equal("1990-01-01", items[0].DateOfBirth);

        Assert.Equal(2, items[1].Id);
        Assert.Equal("Marie", items[1].FirstName);
    }

    [Fact]
    public async Task Can_Capture_Unmapped_Columns_In_ExtensionData()
    {
        // Les colonnes "age", "city" et "score" n'existent pas dans le modèle ExtensionDataRecord
        string csv = 
            "id,name,age,city,score\r\n" +
            "1,Paul,30,Paris,99\r\n" +
            "2,Anna,,Lyon,85\r\n"; // Anna n'a pas d'âge (vide)

        using var readStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var items = new List<ExtensionDataRecord>();

        await foreach (var row in FlashCsvSerializer.DeserializeAsync<ExtensionDataRecord>(readStream))
        {
            items.Add(row);
        }

        Assert.Equal(2, items.Count);

        // Vérification du mapping standard
        Assert.Equal(1, items[0].Id);
        Assert.Equal("Paul", items[0].Name);

        // Vérification du dictionnaire [CsvExtensionData]
        var extraPaul = items[0].ExtraColumns;
        Assert.NotNull(extraPaul);
        Assert.Equal(3, extraPaul.Count);
        Assert.Equal("30", extraPaul["age"]);
        Assert.Equal("Paris", extraPaul["city"]);
        Assert.Equal("99", extraPaul["score"]);

        // Vérification des données vides
        var extraAnna = items[1].ExtraColumns;
        Assert.NotNull(extraAnna);
        Assert.Equal("", extraAnna["age"]); // La cellule vide donne une chaîne vide
        Assert.Equal("Lyon", extraAnna["city"]);
    }
}
