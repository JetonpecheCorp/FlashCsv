using System.Data;
using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests;

[CsvSerializable]
public partial record struct DbPersonRecord
{
    public int Id { get; init; }
    public string Name { get; init; }
    public decimal Score { get; init; }
}

public class CsvDataReaderTests
{
    [Fact]
    public async Task DataReader_Should_Stream_Data_Correctly()
    {
        string csv = 
            "Id,Name,Score\r\n" +
            "1,Alice,99.5\r\n" +
            "2,Bob,85.0\r\n";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // 1. Création du flux asynchrone
        var asyncEnumerable = FlashCsvSerializer.ReadAllAsync<DbPersonRecord>(stream);

        // 2. Wrap dans le DbDataReader
        await using var reader = new CsvDataReader<DbPersonRecord>(asyncEnumerable);

        // Vérification des métadonnées du schéma
        Assert.Equal(3, reader.FieldCount);
        Assert.Equal("Id", reader.GetName(0));
        Assert.Equal("Name", reader.GetName(1));
        Assert.Equal("Score", reader.GetName(2));

        Assert.Equal(0, reader.GetOrdinal("Id"));
        Assert.Equal(typeof(int), reader.GetFieldType(0));

        // 3. Lecture Ligne 1
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetValue(0));
        Assert.Equal("Alice", reader.GetString(1));
        Assert.Equal(99.5m, reader.GetDecimal(2));

        // 4. Lecture Ligne 2
        Assert.True(await reader.ReadAsync());
        Assert.Equal(2, reader.GetInt32(0)); // Test GetInt32
        Assert.Equal("Bob", reader[1]);      // Test Indexeur par ordinal
        Assert.Equal(85.0m, reader["Score"]); // Test Indexeur par nom

        // 5. Fin du flux
        Assert.False(await reader.ReadAsync());

        // Fermeture
        await reader.CloseAsync();
        Assert.True(reader.IsClosed);
    }
}
