using CsvHelper;
using AnomalyDetection.Model;
using AnomalyDetection.ViewModel;
using System.Linq;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Collections.Generic;

var filePath = Path.Combine("data", "wateralarm-vives.csv");
var fullPath = Path.GetFullPath(filePath);

List<SensorData> records;

//read the csv file
using (var reader = new StreamReader(filePath, Encoding.UTF8))
using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HeaderValidated = null,
    MissingFieldFound = null
}))
{
    records = csv.GetRecords<SensorData>().ToList();

    //group by sensor code (by Iban)
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
        Console.WriteLine($"First quartile is: {Q1} \r\nThird quartile is: {Q3} \r\nIQR is: {IQR} \r\nSo IQR method checks for values outside of: {Q1 - 1.5 * IQR} and {Q3 + 1.5 * IQR}");

        var srCount = sRecords.Count();
        int i = 0;

        //recognize and print anomalies: run down current sensor records (mainly by Tijl)
        foreach (var record in sRecords)
        {
            bool iqrAnom = ((record.Distance < (Q1 - 1.5 * IQR)) || (record.Distance > (Q3 + 1.5 * IQR)));
            bool avgAnom = false;
            bool isConfirmedAnomaly = false;

            //values need to have 9 previous and 9 following values to check with this method
            if (i >= 9 && i + 9 < srCount)
            {
                var avgb9 = Enumerable.Range(1, 9).Select(n => sRecords[i - n].Distance ?? 0).Average();
                var avga9 = Enumerable.Range(1, 9).Select(n => sRecords[i + n].Distance ?? 0).Average();
                var avgb9c = Math.Abs(Convert.ToDecimal(avgb9 - record.Distance));
                var avga9c = Math.Abs(Convert.ToDecimal(avga9 - record.Distance));

                //if both the avgb9c as well as the avga9c are bigger than or equal to 3x the IQR, flag as anomaly
                if ((avgb9c >= 3 * Convert.ToDecimal(IQR)) && (avga9c >= 3 * Convert.ToDecimal(IQR)))
                    avgAnom = true;
            }

            if (avgAnom && iqrAnom)
            {
                isConfirmedAnomaly = true;
            } else if (iqrAnom)
            {
                Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time = {record.Time}, Distance = {record.Distance}mm (method: IQR)");
            } else if (avgAnom)
            {
                Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time = {record.Time}, Distance = {record.Distance}mm (method: Averages)");
            }

            // fill excel record with the data
            if (isConfirmedAnomaly)
            {
                record.FixedDistance = null; // anomaly → leave blank
            }
            else
            {
                record.FixedDistance = record.Distance; // not anomaly → copy distance
            }

            i++;
        }
    }
}

// Write updated CSV including anomaly column (by Iban)
using (var writer = new StreamWriter(fullPath, false, Encoding.UTF8))
using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
{
    csvWriter.WriteRecords(records);
}

Console.WriteLine("\nCSV file updated with confirmed anomalies.");

//open in excel
string[] possibleExcelPaths = new[]
{
    @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE",
    @"C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE",
    @"C:\Program Files\Microsoft Office\Office16\EXCEL.EXE",
    @"C:\Program Files (x86)\Microsoft Office\Office16\EXCEL.EXE",
    @"C:\Program Files\Microsoft Office\Office15\EXCEL.EXE",
    @"C:\Program Files (x86)\Microsoft Office\Office15\EXCEL.EXE"
    // Add more if needed
};

string excelPath = possibleExcelPaths.FirstOrDefault(File.Exists);

if (excelPath != null)
{
    Process.Start(excelPath, $"\"{fullPath}\"");
    Console.WriteLine("CSV file opened in Excel.");
}
else
{
    Console.WriteLine("Excel executable not found. Please check your Office installation.");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();