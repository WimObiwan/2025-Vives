using CsvHelper;
using AnomalyDetection.Model;

using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations;


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

int i = 0;
var tag = records[0].Tag;

//print the anomalies
foreach (var record in records)
{ //toevoegen: checken tag, iqr per tag berekenen, bools iqr en z, writeline definite anomaly
    if (record.Tag != tag)
    {
        tag = record.Tag;
    }

    if (record != records[0] && i+2 < records.Length) {
        
        Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {record.tags}, Time= {record.time}, Distance = {record.distance} mm");
    }
    i++;
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();