using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Benchmarks;

// --- Modèle de Test ---
[CsvSerializable]
public partial struct SalesRecord
{
    public int Id { get; set; }
    public string Region { get; set; }
    public DateTime Date { get; set; }
    public double Revenue { get; set; }
    public bool IsActive { get; set; }
}

[MemoryDiagnoser] // Mesure les allocations RAM (fondamental pour les gros volumes)
[RankColumn]      // Affiche le classement
public class CsvMassiveVolumeBenchmark
{
    // On teste 100 000 et 1 000 000 de lignes
    [Params(100_000, 1_000_000)]
    public int RowCount { get; set; }

    private string _filePath = default!;

    [GlobalSetup]
    public void Setup()
    {
        _filePath = Path.GetTempFileName();
        using var writer = new StreamWriter(_filePath);
        writer.WriteLine("Id,Region,Date,Revenue,IsActive");

        for (int i = 0; i < RowCount; i++)
        {
            string revenue = (100.50 + i).ToString(CultureInfo.InvariantCulture);
            string date = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            writer.WriteLine($"{i},Europe,{date},{revenue},true");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
    }

    // --- 1. LECTURE (READ) ---

    [Benchmark(Baseline = true, Description = "1. Read - CsvHelper")]
    public async Task<double> Read_CsvHelper()
    {
        double sum = 0;
        using var reader = new StreamReader(_filePath);
        // L'API moderne prend directement le CultureInfo
        await using var csv = CsvHelper.CsvReader.Create(reader, new() { FormatProvider = CultureInfo.InvariantCulture });

        await foreach (var record in csv.GetRecordsAsync<SalesRecord>())
        {
            sum += record.Revenue;
        }
        return sum;
    }

    [Benchmark(Description = "1. Read - Sylvan")]
    public async Task<double> Read_Sylvan()
    {
        double sum = 0;
        await using var reader = await Sylvan.Data.Csv.CsvDataReader.CreateAsync(_filePath);

        while (await reader.ReadAsync())
        {
            // Même travail que FlashCsv : mapping complet du struct
            var record = new SalesRecord
            {
                Id = reader.GetInt32(0),
                Region = reader.GetString(1),
                Date = reader.GetDateTime(2),
                Revenue = reader.GetDouble(3),
                IsActive = reader.GetBoolean(4)
            };
            sum += record.Revenue;
        }
        return sum;
    }

    [Benchmark(Description = "1. Read - FlashCsv")]
    public async Task<double> Read_FlashCsv()
    {
        double sum = 0;
        // Tampon de 1 Mo (1 048 576 octets)
        var options = new CsvReaderOptions
        { 
            BufferSize = 1024 * 1024,
            UseStringPooling = false
        };

        await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, FileOptions.SequentialScan);
        await using var reader = await CsvStreamReader.CreateAsync(stream, options).ConfigureAwait(false);

        string[] headers = await reader.ReadHeaderAsync().ConfigureAwait(false);
        int[] colMap = SalesRecord.CreateColumnMap(headers);

        while (true)
        {
            while (reader.TryReadNextRowSync<SalesRecord>(colMap, out bool success, out var record, out _))
            {
                if (success)
                {
                    sum += record.Revenue;
                }
            }

            if (!await reader.ReadMoreDataAsync().ConfigureAwait(false))
            {
                break;
            }
        }

        return sum;
    }

    // --- 2. ECRITURE (WRITE) ---

    [Benchmark(Description = "2. Write - CsvHelper")]
    public async Task Write_CsvHelper()
    {
        var data = Enumerable.Repeat(new SalesRecord { Id = 1, Region = "Asia", Date = DateTime.UtcNow, Revenue = 99.9, IsActive = true }, RowCount);

        await using var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await using var csv = CsvHelper.CsvWriter.Create(writer, new() { FormatProvider = CultureInfo.InvariantCulture });

        await csv.WriteRecordsAsync(data);
    }

    [Benchmark(Description = "2. Write - FlashCsv")]
    public async Task Write_FlashCsv()
    {
        var data = Enumerable.Repeat(new SalesRecord { Id = 1, Region = "Asia", Date = DateTime.UtcNow, Revenue = 99.9, IsActive = true }, RowCount);

        // Stream.Null absorbe les octets sans rien allouer
        await FlashCsvSerializer.SerializeAsync(Stream.Null, data);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<CsvMassiveVolumeBenchmark>();
    }
}