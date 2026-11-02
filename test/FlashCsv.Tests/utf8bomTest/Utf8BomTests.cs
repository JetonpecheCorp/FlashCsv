
using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.utf8bomTest;

[CsvSerializable]
public partial record struct ClientTest
{
    [CsvName("Id")]
    public int Id { get; init; }

    [CsvName("Nom")]
    public string Nom { get; init; }

    [CsvName("Solde")]
    public double Solde { get; init; }
}

public class Utf8BomTests
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    [Fact]
    public async Task Read_WithExcelUtf8Bom_ShouldSkipBomAndParseFirstHeaderCorrectly()
    {
        string contenuCsv = "Id,Nom,Solde\r\n1,Alice,1500.50\r\n2,Bob,2400.00";
        byte[] csvBytes = Encoding.UTF8.GetBytes(contenuCsv);

        byte[] csvAvecBom = new byte[Utf8Bom.Length + csvBytes.Length];
        Buffer.BlockCopy(Utf8Bom, 0, csvAvecBom, 0, Utf8Bom.Length);
        Buffer.BlockCopy(csvBytes, 0, csvAvecBom, Utf8Bom.Length, csvBytes.Length);

        await using var stream = new MemoryStream(csvAvecBom);
        var options = new CsvReaderOptions
        {
            DetectAndSkipBom = true
        };

        var resultats = new List<ClientTest>();
        await foreach (var item in FlashCsvSerializer.DeserializeAsync<ClientTest>(stream, options))
        {
            resultats.Add(item);
        }

        Assert.Equal(2, resultats.Count);
        Assert.Equal(1, resultats[0].Id);
        Assert.Equal("Alice", resultats[0].Nom);
        Assert.Equal(1500.50, resultats[0].Solde);

        Assert.Equal(2, resultats[1].Id);
        Assert.Equal("Bob", resultats[1].Nom);
    }

    [Fact]
    public async Task Read_WithoutBom_ShouldParseNormally()
    {
        string contenuCsv = "Id,Nom,Solde\r\n10,Martin,50.0";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(contenuCsv));
        var options = new CsvReaderOptions
        {
            DetectAndSkipBom = true // Doit fonctionner de manière transparente même sans BOM
        };

        var resultats = new List<ClientTest>();
        await foreach (var item in FlashCsvSerializer.DeserializeAsync<ClientTest>(stream, options))
        {
            resultats.Add(item);
        }

        Assert.Single(resultats);
        Assert.Equal(10, resultats[0].Id);
        Assert.Equal("Martin", resultats[0].Nom);
    }

    [Fact]
    public async Task Write_WithWriteBomTrue_ShouldPrefixStreamWithUtf8Bom()
    {
        var clients = new[]
        {
            new ClientTest { Id = 1, Nom = "Éléonore", Solde = 99.9 }
        };

        var options = new CsvWriterOptions
        {
            WriteHeader = true,
            WriteBom = true
        };

        using var ms = new MemoryStream();

        await FlashCsvSerializer.SerializeAsync(ms, clients, options);
        byte[] output = ms.ToArray();

        // Les 3 premiers octets doivent être strictement le BOM UTF-8
        Assert.True(output.Length >= 3);
        Assert.Equal(0xEF, output[0]);
        Assert.Equal(0xBB, output[1]);
        Assert.Equal(0xBF, output[2]);

        // Le reste doit correspondre au texte avec accents
        string csvText = Encoding.UTF8.GetString(output.AsSpan(3));
        Assert.StartsWith("Id,Nom,Solde", csvText);
        Assert.Contains("Éléonore", csvText);
    }

    [Fact]
    public async Task Write_WithWriteBomFalse_ShouldNotIncludeBom()
    {
        var clients = new[]
        {
            new ClientTest { Id = 1, Nom = "Test", Solde = 10.0 }
        };

        var options = new CsvWriterOptions
        {
            WriteHeader = true,
            WriteBom = false
        };

        await using var ms = new MemoryStream();

        await FlashCsvSerializer.SerializeAsync(ms, clients, options);
        byte[] output = ms.ToArray();

        // Les premiers octets doivent être directement le premier caractère 'I' (0x49)
        Assert.True(output.Length > 0);
        Assert.NotEqual(0xEF, output[0]);
        Assert.Equal((byte)'I', output[0]);
    }

    [Fact]
    public async Task Roundtrip_WriteWithBom_ThenReadWithBom_ShouldPreserveData()
    {
        // GIVEN : Écriture avec BOM
        var originaux = new[]
        {
            new ClientTest { Id = 100, Nom = "Émilie", Solde = 1234.56 },
            new ClientTest { Id = 101, Nom = "Benoît", Solde = 789.10 }
        };

        await using var ms = new MemoryStream();

        // leaveOpen: true empêche CsvStreamWriter de fermer le MemoryStream à la fin
        await FlashCsvSerializer.SerializeAsync(
            ms,
            originaux,
            new CsvWriterOptions
            {
                WriteBom = true,
                WriteHeader = true
            }, 
            leaveOpen: true);

        // Rembobinage du flux pour lecture
        ms.Position = 0;

        // Relecture avec détection du BOM active
        var lus = new List<ClientTest>();
        await foreach (var item in FlashCsvSerializer.DeserializeAsync<ClientTest>(
            ms, 
            new CsvReaderOptions { DetectAndSkipBom = true },
            leaveOpen: false))
        {
            lus.Add(item);
        }

        Assert.Equal(2, lus.Count);
        Assert.Equal(originaux[0], lus[0]);
        Assert.Equal(originaux[1], lus[1]);
    }
}
