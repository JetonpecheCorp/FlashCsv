using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct DynamicImportRecord
{
    [CsvName("default_id")]
    public int Id { get; init; }

    [CsvName("default_email", "mail")]
    public string Email { get; init; }

    [CsvName("default_amount")]
    public double Amount { get; init; }

    public bool IsActive { get; init; }
}

public class DynamicColumnMappingTests
{
    [Fact]
    public async Task DynamicMapping_Maps_Custom_Header_Names_To_Model_Properties()
    {
        // En-têtes CSV venant d'un système tiers (totalement différents des noms C# et de [CsvName])
        string csv = 
            "IdentifiantExterne;CourrielUtilisateur;MontantTotal;StatutCompte\r\n" +
            "42;jean.dupont@test.com;1499,99;true\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var options = new CsvReaderOptions
        {
            Delimiter = ';',
            ColumnMapping = new Dictionary<string, string>
            {
                ["Id"] = "IdentifiantExterne",
                ["Email"] = "CourrielUtilisateur",
                ["Amount"] = "MontantTotal",
                ["IsActive"] = "StatutCompte"
            }
        };

        var results = await FlashCsvSerializer.DeserializeAsync<DynamicImportRecord>(csv, options);

        Assert.Single(results);
        Assert.Equal(42, results[0].Id);
        Assert.Equal("jean.dupont@test.com", results[0].Email);
        Assert.Equal(1499.99, results[0].Amount);
        Assert.True(results[0].IsActive);
    }

    [Fact]
    public async Task DynamicMapping_Overrides_CsvName_Attribute()
    {
        // Le modèle a [CsvName("default_email")], mais le CSV contient à la fois "default_email" et "override_mail"
        // Le mapping dynamique doit pointer vers "override_mail"
        string csv = 
            "default_id,default_email,override_mail,default_amount,IsActive\r\n" +
            "1,ignore@test.com,target@test.com,10.5,true\r\n";

        var options = new CsvReaderOptions
        {
            ColumnMapping = new Dictionary<string, string>
            {
                ["Email"] = "override_mail"
            }
        };

        var results = await FlashCsvSerializer.DeserializeAsync<DynamicImportRecord>(csv, options);

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("target@test.com", results[0].Email);
        Assert.Equal(10.5, results[0].Amount);
    }

    [Fact]
    public async Task DynamicMapping_Is_Case_Insensitive()
    {
        // L'en-tête CSV est en minuscules ou majuscules mélangées
        string csv = 
            "colonne_id,courriel_client,montant,actif\r\n" +
            "99,test@domain.com,200.0,true\r\n";

        var options = new CsvReaderOptions
        {
            ColumnMapping = new Dictionary<string, string>
            {
                ["Id"] = "COLONNE_ID",
                ["Email"] = "COURRIEL_CLIENT",
                ["Amount"] = "MONTANT",
                ["IsActive"] = "ACTIF"
            }
        };

        var results = await FlashCsvSerializer.DeserializeAsync<DynamicImportRecord>(csv, options);

        Assert.Single(results);
        Assert.Equal(99, results[0].Id);
        Assert.Equal("test@domain.com", results[0].Email);
    }

    [Fact]
    public async Task DynamicMapping_Allows_Partial_Mapping_And_Falls_Back_To_Defaults()
    {
        // Seulement "Email" est mappé dynamiquement vers "Courriel", 
        // les autres colonnes utilisent leurs alias naturels ou [CsvName]
        string csv = 
            "default_id,Courriel,default_amount,IsActive\r\n" +
            "7,fallback@test.com,55.0,false\r\n";

        var options = new CsvReaderOptions
        {
            ColumnMapping = new Dictionary<string, string>
            {
                ["Email"] = "Courriel"
            }
        };

        var results = await FlashCsvSerializer.DeserializeAsync<DynamicImportRecord>(csv, options);

        Assert.Single(results);
        Assert.Equal(7, results[0].Id);
        Assert.Equal("fallback@test.com", results[0].Email);
        Assert.Equal(55.0, results[0].Amount);
        Assert.False(results[0].IsActive);
    }
}