using System.Text;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace FlashCsv.Tests.FonctionAvancerTest;

[CsvSerializable]
[CsvDiscriminator("type")] // La colonne qui détermine le type d'objet
[CsvDerivedType(typeof(CarRecord), "car")]
[CsvDerivedType(typeof(BikeRecord), "bike")]
public abstract partial record VehicleRecord
{
    public string Brand { get; init; } = default!;
}

[CsvSerializable]
public partial record CarRecord : VehicleRecord
{
    public int Doors { get; init; }
}

[CsvSerializable]
public partial record BikeRecord : VehicleRecord
{
    public bool HasSidecar { get; init; }
}

public class PolymorphismTests
{
    [Fact]
    public async Task Can_Deserialize_Heterogeneous_Csv_Using_Discriminator()
    {
        // Fichier contenant un mix de voitures et de motos
        string csv = 
            "type,Brand,Doors,HasSidecar\r\n" +
            "car,Toyota,5,\r\n" +
            "bike,Harley,,true\r\n" +
            "car,Renault,3,false\r\n"; // Renault a "false" pour sidecar, mais ignoré car c'est une voiture

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var items = new List<VehicleRecord>();

        // On parse en demandant le type de base abstrait !
        await foreach (var row in FlashCsvSerializer.DeserializeAsync<VehicleRecord>(stream))
        {
            items.Add(row);
        }

        Assert.Equal(3, items.Count);

        // Ligne 1 : C'est une voiture
        Assert.IsType<CarRecord>(items[0]);
        var car1 = (CarRecord)items[0];
        Assert.Equal("Toyota", car1.Brand);
        Assert.Equal(5, car1.Doors);

        // Ligne 2 : C'est une moto
        Assert.IsType<BikeRecord>(items[1]);
        var bike1 = (BikeRecord)items[1];
        Assert.Equal("Harley", bike1.Brand);
        Assert.True(bike1.HasSidecar);

        // Ligne 3 : C'est une voiture
        Assert.IsType<CarRecord>(items[2]);
        var car2 = (CarRecord)items[2];
        Assert.Equal("Renault", car2.Brand);
        Assert.Equal(3, car2.Doors);
    }
}
