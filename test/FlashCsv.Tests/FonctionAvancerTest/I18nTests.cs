using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.FonctionAvancerTest;

[CsvSerializable]
public partial record struct I18nRecord
{
    // Le premier alias ("id") sera utilisé par défaut pour l'export.
    // Les autres ("identifiant", "codigo") permettront au Reader de mapper automatiquement.
    [CsvName("id", "identifiant", "codigo")]
    public int Id { get; init; }

    [CsvName("first_name", "prenom", "nombre")]
    public string FirstName { get; init; }

    [CsvName("date_of_birth", "date_de_naissance", "fecha_de_nacimiento")]
    public string DateOfBirth { get; init; }
}

public class I18nTests
{
    [Fact]
    public async Task Reader_Can_Map_Multiple_Languages_Automatically()
    {
        // Fichier CSV reçu avec des en-têtes en Français
        string csvFr = 
            "identifiant,prenom,date_de_naissance\r\n" +
            "1,Jean,1990-01-01\r\n";

        using var streamFr = new MemoryStream(Encoding.UTF8.GetBytes(csvFr));
        var items = new List<I18nRecord>();

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<I18nRecord>(streamFr))
        {
            items.Add(row);
        }

        Assert.Single(items);
        Assert.Equal(1, items[0].Id);
        Assert.Equal("Jean", items[0].FirstName);
        Assert.Equal("1990-01-01", items[0].DateOfBirth);
    }

    [Fact]
    public async Task Writer_Can_Translate_Headers_Dynamically()
    {
        var items = new List<I18nRecord>
        {
            new I18nRecord { Id = 2, FirstName = "Alice", DateOfBirth = "2000-02-02" }
        };

        // Dictionnaire de traduction (Anglais -> Espagnol)
        // Les clés correspondent au premier alias défini dans [CsvName]
        var translationsEs = new Dictionary<string, string>
        {
            { "id", "codigo" },
            { "first_name", "nombre" },
            { "date_of_birth", "fecha_de_nacimiento" }
        };

        var options = new CsvWriterOptions
        {
            // Traduit l'en-tête, ou garde le nom d'origine si inconnu
            FormatHeader = original => translationsEs.TryGetValue(original, out var traduit) ? traduit : original
        };

        using var writeStream = new MemoryStream();
        await using var writer = new CsvStreamWriter(writeStream, options);

        // 1. Écriture de l'en-tête (qui passera par FormatHeader)
        await writer.WriteHeaderAsync(I18nRecord.GetHeaderNames());

        // 2. Écriture des données
        foreach (var item in items)
        {
            await item.SerializeAsync(writer);
        }
        await writer.FlushAsync();
        
        string outputCsv = Encoding.UTF8.GetString(writeStream.ToArray());

        // Vérification que les en-têtes ont bien été traduits en espagnol
        Assert.StartsWith("codigo,nombre,fecha_de_nacimiento\r\n", outputCsv);
        
        // Vérification que les données sont intactes
        Assert.Contains("2,Alice,2000-02-02\r\n", outputCsv);
    }

    [Fact]
    public async Task Reader_Emits_Error_If_Translated_Header_Is_Missing_Or_Misspelled()
    {
        // "nombre" est correct, mais "fecha_nacimiento" est mal orthographié (il manque le "_de_")
        // et "identifiant" est absent.
        string csvEs = 
            "nombre,fecha_nacimiento\r\n" +
            "Carlos,1995-05-05\r\n";

        using var streamEs = new MemoryStream(Encoding.UTF8.GetBytes(csvEs));
        
        CsvError? capturedError = null;
        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => capturedError = err
        };

        var items = new List<I18nRecord>();

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<I18nRecord>(streamEs, options))
        {
            items.Add(row);
        }

        // La ligne n'est pas lue car une colonne obligatoire manque
        Assert.Empty(items);
        Assert.NotNull(capturedError);
        
        // Le parseur signale que la colonne "Id" (ou son alias) est introuvable
        Assert.Equal(CsvErrorCode.RequiredFieldMissing, capturedError.Value.Code);
        Assert.Equal("Id", capturedError.Value.ColumnName);
    }

    [Fact]
    public async Task Writer_Falls_Back_To_Original_If_Translation_Is_Missing_Or_Null()
    {
        var items = new List<I18nRecord>
        {
            new I18nRecord { Id = 3, FirstName = "Bob", DateOfBirth = "1980-08-08" }
        };

        // Dictionnaire incomplet : il manque la traduction pour "date_of_birth"
        var translationsIt = new Dictionary<string, string>
        {
            { "id", "codice" },
            { "first_name", "nome" }
            // "date_of_birth" est manquant !
        };

        var options = new CsvWriterOptions
        {
            // Si TryGetValue échoue, il renvoie null, mais le Writer est protégé et reprendra l'original
            FormatHeader = original => translationsIt.TryGetValue(original, out var traduit) ? traduit : null!
        };

        using var writeStream = new MemoryStream();
        await using var writer = new CsvStreamWriter(writeStream, options);

        await writer.WriteHeaderAsync(I18nRecord.GetHeaderNames());
        foreach (var item in items)
        {
            await item.SerializeAsync(writer);
        }
        await writer.FlushAsync();
        
        string outputCsv = Encoding.UTF8.GetString(writeStream.ToArray());

        // L'en-tête doit mélanger l'Italien (traduit) et l'Anglais (repli automatique pour la date)
        Assert.StartsWith("codice,nome,date_of_birth\r\n", outputCsv);
        Assert.Contains("3,Bob,1980-08-08\r\n", outputCsv);
    }
}
