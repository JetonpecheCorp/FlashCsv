using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.EnumTest;

public class EnumCustomMemberTests
{
    [Fact]
    public async Task Deserialize_Accepts_PrimaryName_And_Aliases()
    {
        // Teste : nom officiel, alias anglais, alias alternatif, et valeur null/vide
        string csv = 
            "id,priority,opt_priority\r\n" +
            "1,faible,high\r\n" +
            "2,medium,urgent\r\n" +
            "3,3,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var results = new List<TaskRecord>();

        await foreach (var row in FlashCsvSerializer.ReadAllAsync<TaskRecord>(stream))
        {
            results.Add(row);
        }

        Assert.Equal(3, results.Count);

        // Ligne 1 : "faible" (nom principal) et "high" (alias)
        Assert.Equal(TaskPriority.Low, results[0].Priority);
        Assert.Equal(TaskPriority.High, results[0].OptionalPriority);

        // Ligne 2 : "medium" (alias) et "urgent" (alias)
        Assert.Equal(TaskPriority.Medium, results[1].Priority);
        Assert.Equal(TaskPriority.High, results[1].OptionalPriority);

        // Ligne 3 : "3" (alias numérique) et cellule vide
        Assert.Equal(TaskPriority.High, results[2].Priority);
        Assert.Null(results[2].OptionalPriority);
    }

    [Fact]
    public async Task Serialize_Uses_Primary_Custom_Name()
    {
        var records = new[]
        {
            new TaskRecord
            {
                Id = 1,
                Priority = TaskPriority.Low,
                OptionalPriority = TaskPriority.High
            },
            new TaskRecord
            {
                Id = 2,
                Priority = TaskPriority.Medium,
                OptionalPriority = null
            }
        };

        using var stream = new MemoryStream();
        await FlashCsvSerializer.SerializeAsync(stream, records);

        string csvOutput = Encoding.UTF8.GetString(stream.ToArray());

        // Doit sérialiser le premier libellé "faible", "eleve", "moyen" et non pas "Low", "High", "Medium"
        Assert.Contains("1,faible,eleve", csvOutput);
        Assert.Contains("2,moyen,", csvOutput);
    }

    [Fact]
    public async Task Deserialize_Throws_Error_On_Unknown_Value()
    {
        string csv = 
            "id,priority,opt_priority\r\n" +
            "1,inconnu,\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        CsvError? error = null;

        var options = new CsvReaderOptions
        {
            ErrorMode = CsvErrorMode.FailFast,
            OnError = err => error = err
        };

        var results = new List<TaskRecord>();
        await foreach (var row in FlashCsvSerializer.ReadAllAsync<TaskRecord>(stream, options))
        {
            results.Add(row);
        }

        Assert.Empty(results);
        Assert.NotNull(error);
        Assert.Equal(CsvErrorCode.InvalidEnum, error.Value.Code);
        Assert.Equal("Priority", error.Value.ColumnName);
        Assert.Equal("inconnu", error.Value.RawText);
    }
}
