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
    var sRecords = group.OrderBy(r => r.Time).ToList();

    Console.WriteLine($"\n Sensor: {sensorId}");

    List<double> distances = sRecords.Select(r => (double)r.Distance.GetValueOrDefault()).ToList();
    distances.Sort();
    double Q1 = QuartileVM.GetPercentile(distances, 25);
    double Q3 = QuartileVM.GetPercentile(distances, 75);
    double IQR = Q3 - Q1;
    Console.WriteLine($"First quartile is: {Q1} \r\nThird quartile is: {Q3} \r\nIQR is: {IQR} \r\nSo we check for: {Q1 - 1.5 * IQR} or {Q3 + 1.5 * IQR}");
    var srCount = sRecords.Count();
    int i = 0;

    // !! toevoegen: bools iqr en z, writeline definite anomaly !!

    //print the anomalies
    foreach (var record in sRecords)
    {
        bool z = false;
        if (record != sRecords[0] && i + 1 < srCount)
        {
            var ba = (sRecords[i - 1].Distance + sRecords[i + 1].Distance) / 2;
            var bc = (sRecords[i - 1].Distance + record.Distance) / 2;
            var ca = (sRecords[i + 1].Distance + record.Distance) / 2;
            Console.WriteLine($"c= {record.Distance} ba= {ba} bc= {bc} ca= {ca}");
            if (i >= 9 && i + 9 < srCount)
            {
                var gem9 = (sRecords[i - 1].Distance + sRecords[i - 2].Distance + sRecords[i - 3].Distance + sRecords[i - 4].Distance + sRecords[i - 5].Distance + sRecords[i - 6].Distance + sRecords[i - 7].Distance + sRecords[i - 8].Distance + sRecords[i - 9].Distance) / 9;
                Console.WriteLine($"gem9= {gem9} gem9-c= {gem9 - record.Distance} c-gem9={record.Distance - gem9}");
                var gem9c = Math.Abs(Convert.ToDecimal(gem9 - record.Distance));
                var gep9 = (sRecords[i + 1].Distance + sRecords[i + 2].Distance + sRecords[i + 3].Distance + sRecords[i + 4].Distance + sRecords[i + 5].Distance + sRecords[i + 6].Distance + sRecords[i + 7].Distance + sRecords[i + 8].Distance + sRecords[i + 9].Distance) / 9;
                Console.WriteLine($"gep9= {gep9} gep9-c= {gep9 - record.Distance} c-gep9={record.Distance - gep9}");
                var gep9c = Math.Abs(Convert.ToDecimal(gep9 - record.Distance));
                
                if ((gem9c >= 3 * Convert.ToDecimal(IQR)) && (gep9c >= 3 * Convert.ToDecimal(IQR))) z = true;
            }

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