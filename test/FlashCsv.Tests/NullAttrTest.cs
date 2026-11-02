using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests;

public class NullAttrTest
{
    [Fact]
    public async Task Can_Handle_Custom_Null_Values_For_Nullable_Properties()
    {
        string csv = 
            "id,description,age,required_val\r\n" +
            "1,VIDE,-1,42\r\n" +
            "2,AUCUN,25,99\r\n" +
            "3,Texte,INCONNU,10\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var rows = new List<NullValuesRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<NullValuesRecord>(stream))
        {
            rows.Add(row);
        }

        Assert.Equal(3, rows.Count);

        // Ligne 1 : "VIDE" -> null, "-1" -> null
        Assert.Null(rows[0].Description);
        Assert.Null(rows[0].Age);
        Assert.Equal(42, rows[0].RequiredVal);

        // Ligne 2 : "AUCUN" -> null, "25" -> 25
        Assert.Null(rows[1].Description);
        Assert.Equal(25, rows[1].Age);

        // Ligne 3 : "Texte" -> "Texte", "INCONNU" -> null
        Assert.Equal("Texte", rows[2].Description);
        Assert.Null(rows[2].Age);
    }

    [Fact]
    public async Task Emits_RequiredFieldMissing_When_Non_Nullable_Field_Matches_Null_Value()
    {
        // "NON_RENSEIGNE" est marqué comme valeur nulle sur 'required_val' (int non nullable)
        string csv = 
            "id,description,age,required_val\r\n" +
            "1,Valide,20,NON_RENSEIGNE\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        CsvError? capturedError = null;
        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => capturedError = err
        };

        var rows = new List<NullValuesRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<NullValuesRecord>(stream, options))
        {
            rows.Add(row);
        }

        Assert.Empty(rows);
        Assert.NotNull(capturedError);
        Assert.Equal(2, capturedError.Value.LineNumber);
        Assert.Equal("RequiredVal", capturedError.Value.ColumnName);
        Assert.Equal(CsvErrorCode.RequiredFieldMissing, capturedError.Value.Code);
        Assert.Equal("NON_RENSEIGNE", capturedError.Value.RawText);
    }
}
