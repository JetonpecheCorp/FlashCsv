using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests;

public class BoolAttrTest
{
    [Fact]
    public async Task Can_Parse_Custom_Boolean_Representations()
    {
        string csv = 
            "id,is_active,is_verified\r\n" +
            "1,ACTIF,oui\r\n" +
            "2,INACTIF,non\r\n" +
            "3,VRAI,true\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = new List<CustomBoolRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<CustomBoolRecord>(stream))
        {
            rows.Add(row);
        }

        Assert.Equal(3, rows.Count);

        // Ligne 1
        Assert.True(rows[0].IsActive);
        Assert.True(rows[0].IsVerified);

        // Ligne 2
        Assert.False(rows[1].IsActive);
        Assert.False(rows[1].IsVerified);

        // Ligne 3
        Assert.True(rows[2].IsActive);
        Assert.True(rows[2].IsVerified);
    }

    [Fact]
    public async Task Ignores_Case_For_Custom_Boolean_Literals()
    {
        string csv = 
            "id,is_active,is_verified\r\n" +
            "1,actif,OUI\r\n" +
            "2,InActif,Non\r\n" +
            "3,vRaI,TRUE\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = new List<CustomBoolRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<CustomBoolRecord>(stream))
        {
            rows.Add(row);
        }

        Assert.Equal(3, rows.Count);
        Assert.True(rows[0].IsActive);
        Assert.True(rows[0].IsVerified);

        Assert.False(rows[1].IsActive);
        Assert.False(rows[1].IsVerified);

        Assert.True(rows[2].IsActive);
        Assert.True(rows[2].IsVerified);
    }

    [Fact]
    public async Task Can_Use_Single_True_List_With_Implicit_False_Fallback()
    {
        string csv = 
            "id,flag\r\n" +
            "1,OUI\r\n" +
            "2,1\r\n" +
            "3,NON\r\n" +
            "4,autre_valeur\r\n" +
            "5,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = new List<FeatureRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<FeatureRecord>(stream))
        {
            rows.Add(row);
        }

        Assert.Equal(5, rows.Count);
        Assert.True(rows[0].IsEnabled);  // "OUI"
        Assert.True(rows[1].IsEnabled);  // "1"
        Assert.False(rows[2].IsEnabled); // "NON"          -> faux par repli
        Assert.False(rows[3].IsEnabled); // "autre_valeur" -> faux par repli
        Assert.False(rows[4].IsEnabled); // champ vide     -> faux par repli
    }

    [Fact]
    public async Task Reports_InvalidBoolean_When_Literal_Is_Not_Recognized()
    {
        // "ENABLED" n'est ni dans ["VRAI", "ACTIF", "1"], ni dans ["FAUX", "INACTIF", "0"]
        string csv = 
            "id,is_active,is_verified\r\n" +
            "1,ENABLED,oui\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        CsvError? capturedError = null;
        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => capturedError = err
        };

        var rows = new List<CustomBoolRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<CustomBoolRecord>(stream, options))
        {
            rows.Add(row);
        }

        Assert.Empty(rows);
        Assert.NotNull(capturedError);
        Assert.Equal(2, capturedError.Value.LineNumber);
        Assert.Equal("IsActive", capturedError.Value.ColumnName);
        Assert.Equal(CsvErrorCode.InvalidBoolean, capturedError.Value.Code);
        Assert.Equal("ENABLED", capturedError.Value.RawText);
    }
}
