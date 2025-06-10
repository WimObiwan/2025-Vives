using CsvHelper;
using AnomalyDetection.Model;
using AnomalyDetection.ViewModel;
using System.Linq;
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
//foreach (var record in records)
//{
//    Console.WriteLine($"Tag = {record.tags}, Distance = {record.distance} mm");
//}

//group by sensor code iban
var groupedBySensor = records
    .GroupBy(r => r.Tags)
    .ToList();

foreach (var group in groupedBySensor)
{
    var sensorId = group.Key;
    var sortedRecords = group.OrderBy(r => r.Time).ToList();

    Console.WriteLine($"\n Sensor: {sensorId}");

    List<double> distances = sortedRecords.Select(r => (double)r.Distance.GetValueOrDefault()).ToList();
    distances.Sort();
    double Q1 = QuartileVM.GetPercentile(distances, 25);
    double Q3 = QuartileVM.GetPercentile(distances, 75);
    double IQR = Q3 - Q1;
    Console.WriteLine($"First quartile is: {Q1} \r\nThird quartile is: {Q3} \r\nIQR is: {IQR} \r\nSo we check for: {Q1 - 1.5 * IQR} or {Q3 + 1.5 * IQR}");
    int i = 0;

    // !! toevoegen: bools iqr en z, writeline definite anomaly !!

    //print the anomalies
    foreach (var record in sortedRecords)
    {
        bool z = false;
        if (record != sortedRecords[0] && i + 2 < sortedRecords.Count())
        {

            if (z == true && ((record.Distance < (Q1 - 1.5 * IQR)) || (record.Distance > (Q3 + 1.5 * IQR))))
            {
                Console.WriteLine($"!ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (method: z & IQR)");
            } else if ((record.Distance < (Q1 - 1.5 * IQR)) || (record.Distance > (Q3 + 1.5 * IQR)))
            {
                Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (method: IQR)");
            } else if (z == true)
            {
                Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (method: z of avg of zo)");
            }
        } else if ((record.Distance < (Q1 - 1.5 * IQR)) || (record.Distance > (Q3 + 1.5 * IQR)))
        {
            Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (method: IQR)");
        }
        i++;
    }
}
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();