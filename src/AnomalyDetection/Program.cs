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

var records = csv.GetRecords<SensorData>()
    .Where(r => !string.IsNullOrEmpty(r.Tags) && r.Distance.HasValue)
    .ToList();

TimeSpan windowDuration = TimeSpan.FromHours(24);
double zThreshold = 2.5;
double minStdDev = 20.0;
int confirmWindow = 3;

Console.WriteLine($"Rolling Z-Score Anomaly Detection (window: {windowDuration.TotalHours} hours, z > {zThreshold})\n");

var groupedBySensor = records
    .GroupBy(r => r.Tags)
    .ToList();

foreach (var group in groupedBySensor)
{
    var sensorId = group.Key;
    var sorted = group.OrderBy(r => r.Time).ToList();

    Console.WriteLine($"\n Sensor: {sensorId}");

    for (int i = 0; i < sorted.Count; i++)
    {
        var current = sorted[i];

        var window = sorted
            .Where(r => r.Time < current.Time &&
                        r.Time >= current.Time - windowDuration &&
                        r.Distance.HasValue)
            .Select(r => r.Distance.Value)
            .ToList();

        if (window.Count < 3)
            continue;

        double mean = window.Average();
        double stdDev = Math.Sqrt(window.Average(d => Math.Pow(d - mean, 2)));

        if (stdDev < minStdDev)
            continue;

        double z = (current.Distance.Value - mean) / stdDev;

        if (Math.Abs(z) > zThreshold)
        {
            var futureZ = sorted.Skip(i + 1).Take(confirmWindow)
                .Where(r => r.Distance.HasValue)
                .Select(r => (r.Distance.Value - mean) / stdDev)
                .ToList();

            bool returnsToNormal = futureZ.All(fz => Math.Abs(fz) < zThreshold);

            double? deltaBefore = (i > 0) ? sorted[i].Distance - sorted[i - 1].Distance : null;
            double? deltaAfter = (i + 1 < sorted.Count) ? sorted[i + 1].Distance - sorted[i].Distance : null;
            bool vShape = deltaBefore.HasValue && deltaAfter.HasValue &&
                          Math.Sign(deltaBefore.Value) != Math.Sign(deltaAfter.Value);

            if (returnsToNormal || vShape)
            {
                Console.WriteLine($"Anomaly at {current.Time}: Distance = {current.Distance} with tag: {current.Tags} mm (z = {z:F2})");
            }
        }
    }
}

Console.WriteLine("\nDetectie voltooid. Druk op een toets om af te sluiten...");
Console.ReadKey();