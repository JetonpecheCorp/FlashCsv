using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;
using FlashCsv.Tests.RangeTest;

namespace FlashCsv.Tests.DateTest;

[CsvSerializable]
public partial record struct EventRecord
{
    [CsvName("id")]
    public int Id { get; init; }

    // Utilise le format local spécifié : "dd/MM/yyyy"
    [CsvName("local_date")]
    [CsvFormat("dd/MM/yyyy")]
    public DateOnly LocalDate { get; init; }

    // N'a pas d'attribut local : utilise le global des options s'il existe, sinon fallback ISO
    [CsvName("fallback_date")]
    public DateTime FallbackDate { get; init; }
}

public class DateTimeFormatTests
{
    [Fact]
    public async Task Uses_Local_Format_First_Then_Global_Option()
    {
        // local_date est au format dd/MM/yyyy
        // fallback_date est au format yyyy.MM.dd (défini dans options.DateTimeFormat)
        string csv = 
            "id,local_date,fallback_date\r\n" +
            "1,25/12/2026,2026.10.15\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var options = new CsvReaderOptions
        {
            DateTimeFormat = "yyyy.MM.dd" // Format global
        };

        var results = new List<EventRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<EventRecord>(stream, options))
        {
            results.Add(row);
        }

        Assert.Single(results);
        // Validé via le format local "dd/MM/yyyy"
        Assert.Equal(new DateOnly(2026, 12, 25), results[0].LocalDate);
        // Validé via le format global "yyyy.MM.dd"
        Assert.Equal(new DateTime(2026, 10, 15), results[0].FallbackDate);
    }

    [Fact]
    public async Task Falls_Back_To_Iso_When_No_Global_And_No_Local()
    {
        // local_date utilise son format local
        // fallback_date utilise le format ISO 8601 standard car options.DateTimeFormat est null
        string csv = 
            "id,local_date,fallback_date\r\n" +
            "2,01/01/2026,2026-05-20T14:30:00Z\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new CsvReaderOptions(); // Aucun format global

        var results = new List<EventRecord>();
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<EventRecord>(stream, options))
        {
            results.Add(row);
        }

        Assert.Single(results);
        Assert.Equal(new DateOnly(2026, 1, 1), results[0].LocalDate);
        Assert.Equal(new DateTime(2026, 5, 20, 14, 30, 0, DateTimeKind.Utc), results[0].FallbackDate.ToUniversalTime());
    }
}
