

using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Can_Use_Convenient_FlashCsvSerializer_API()
    {
        string inputCsv = 
            "identifiant,nom_complet,Score,date_creation,Roles\r\n" +
            "10,Bob,12.75,2026-10-26T15:30:00.0000000Z,\"[\"\"dev\"\"]\"\r\n" +
            "20,Alice,18.5,2026-10-26T15:30:00.0000000Z,\"[\"\"admin\"\"]\"\r\n";

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(inputCsv));

        // Lecture fluide via IAsyncEnumerable
        var users = new List<UserRecord>();
        await foreach (var user in FlashCsvSerializer.ReadAllAsync<UserRecord>(inputStream))
        {
            users.Add(user);
        }

        Assert.Equal(2, users.Count);
        Assert.Equal("Bob", users[0].Name);
        Assert.Equal("Alice", users[1].Name);

        // Écriture fluide
        using var outputStream = new MemoryStream();
        await FlashCsvSerializer.SerializeAsync(outputStream, users);

        string outputCsv = Encoding.UTF8.GetString(outputStream.ToArray());
        Assert.Contains("nom_complet", outputCsv);
        Assert.Contains("Bob", outputCsv);
    }

    [Fact]
    public async Task Can_Read_And_Map_Csv_With_100_Columns()
    {
        const int totalColumns = 100;

        // 1. Génération d'une ligne d'en-tête de 100 colonnes dans le désordre
        // On place col_99 en début de ligne pour vérifier la résolution d'index
        var headerList = new List<string> { "col_99" };
        for (int i = 0; i < totalColumns - 1; i++)
        {
            headerList.Add($"col_{i}");
        }
        string headerLine = string.Join(",", headerList);

        // 2. Génération des valeurs correspondantes
        var valueList = new List<string> { "ValeurFinale" }; // Valeur de col_99
        for (int i = 0; i < totalColumns - 1; i++)
        {
            if (i == 0) valueList.Add("42");              // col_0 (int)
            else if (i == 1) valueList.Add("TexteDebut");  // col_1 (string)
            else if (i == 50) valueList.Add("123.45");    // col_50 (double)
            else if (i == 64) valueList.Add("6400");      // col_64 (int)
            else valueList.Add($"v_{i}");
        }
        string dataLine = string.Join(",", valueList);

        string csvContent = $"{headerLine}\r\n{dataLine}\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // 3. Lecture via l'API simplifiée FlashCsvSerializer
        var records = new List<WideRecord>();
        await foreach (var item in FlashCsvSerializer.ReadAllAsync<WideRecord>(stream))
        {
            records.Add(item);
        }

        // 4. Assertions
        Assert.Single(records);
        var record = records[0];

        Assert.Equal(42, record.Col0);
        Assert.Equal("TexteDebut", record.Col1);
        Assert.Equal(123.45, record.Col50);
        Assert.Equal(6400, record.Col64);
        Assert.Equal("ValeurFinale", record.Col99);
    }

    [Fact]
    public async Task ErrorMode_FailFast_Stops_At_First_Error_Without_Throwing()
    {
        string csv = 
            "identifiant,nom_complet,Score,date_creation,Roles\r\n" +
            "1,Bob,10.0,2026-10-26T15:30:00.0000000Z,\"[]\"\r\n" +
            "invalide,Alice,20.0,2026-10-26T15:30:00.0000000Z,\"[]\"\r\n" + // Erreur Ligne 3
            "3,Charlie,30.0,2026-10-26T15:30:00.0000000Z,\"[]\"\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        CsvError? capturedError = null;
        var validRows = new List<UserRecord>();

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast, // Doit s'arrêter à la ligne 3
            OnError = err =>
            {
                capturedError = err;
            }
        };

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<UserRecord>(stream, options))
        {
            validRows.Add(row);
        }

        // Seule la première ligne a été lue avant l'arrêt net
        Assert.Single(validRows);
        Assert.Equal("Bob", validRows[0].Name);

        // L'erreur de la 2e ligne de données (ligne physique 3) a bien été capturée
        Assert.NotNull(capturedError);
        Assert.Equal(3, capturedError.Value.LineNumber);
        Assert.Equal(CsvErrorCode.InvalidInteger, capturedError.Value.Code);
        Assert.Equal("Id", capturedError.Value.ColumnName);
    }

    [Fact]
    public async Task ErrorMode_CollectAndContinue_Skips_Bad_Rows_And_Reads_Rest()
    {
        string csv = 
            "identifiant,nom_complet,Score,date_creation,Roles\r\n" +
            "1,Bob,10.0,2026-10-26T15:30:00.0000000Z,\"[]\"\r\n" +
            "invalide,Alice,20.0,2026-10-26T15:30:00.0000000Z,\"[]\"\r\n" + // Ligne 3 erronée
            "3,Charlie,30.0,2026-10-26T15:30:00.0000000Z,\"[]\"\r\n";       // Ligne 4 valide

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var errors = new List<CsvError>();
        var validRows = new List<UserRecord>();

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.CollectAndContinue, // Saute les mauvaises lignes
            OnError = err =>
            {
                errors.Add(err);
            }
        };

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<UserRecord>(stream, options))
        {
            validRows.Add(row);
        }

        // Les deux lignes valides (Bob et Charlie) ont été traitées
        Assert.Equal(2, validRows.Count);
        Assert.Equal("Bob", validRows[0].Name);
        Assert.Equal("Charlie", validRows[1].Name);

        // Une seule erreur a été collectée
        Assert.Single(errors);
        Assert.Equal(3, errors[0].LineNumber);
        Assert.Equal("invalide", Encoding.UTF8.GetString(errors[0].RawValue.Span));
    }
}
