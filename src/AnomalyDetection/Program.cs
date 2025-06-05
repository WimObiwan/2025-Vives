using CsvHelper;
using AnomalyDetection.Model;

using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Text;


var filePath = Path.Combine("data", "wateralarm-vives.csv");

using var reader = new StreamReader(filePath, Encoding.UTF8);
using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HeaderValidated = null,
    MissingFieldFound = null
});

var records = csv.GetRecords<SensorData>().ToList();

// Print the records
foreach (var record in records)
{
    Console.WriteLine($"Tag = {record.tags}, Distance = {record.distance} mm");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();