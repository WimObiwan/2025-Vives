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

//group by sensor code (by Iban)
var groupedBySensor = records
    .GroupBy(r => r.Tags)
    .ToList();

foreach (var group in groupedBySensor)
{
    var sensorId = group.Key;
    var sRecords = group.OrderBy(r => r.Time).ToList(); //sRecords = sorted records
    
    //name sensor current values
    Console.WriteLine($"\n Sensor: {sensorId}");

    //IQR: calculate quartiles and interquartile range
    List<double> distances = sRecords.Select(r => (double)r.Distance.GetValueOrDefault()).ToList();
    distances.Sort();
    double Q1 = QuartileVM.GetPercentile(distances, 25);
    double Q3 = QuartileVM.GetPercentile(distances, 75);
    double IQR = Q3 - Q1;
    Console.WriteLine($"First quartile is: {Q1} \r\nThird quartile is: {Q3} \r\nIQR is: {IQR} \r\nSo we check for: {Q1 - 1.5 * IQR} or {Q3 + 1.5 * IQR}");
    
    //set length of sensor records list and increment integer for foreach 
    var srCount = sRecords.Count();
    int i = 0;

    //recognize and print anomalies: run down current sensor records
    foreach (var record in sRecords)
    {
        //bool to keep track whether my calculation flags the current value as anomaly
        bool avgAnom = false; //avgAnom = anomaly based on averages

        //first and last value can't be compared with both the previous as well as the next value
        if (record != sRecords[0] && i + 1 < srCount)
        {
            //c= current value; b = value before current; a = value after current
            var ba = (sRecords[i - 1].Distance + sRecords[i + 1].Distance) / 2;
            var bc = (sRecords[i - 1].Distance + record.Distance) / 2;
            var ca = (sRecords[i + 1].Distance + record.Distance) / 2;
            //average of ba, bc and ca calculated to compare and possibly use (currently unused)
            Console.WriteLine($"c= {record.Distance} ba= {ba} bc= {bc} ca= {ca}");

            //scope widened: values need to have 9 previous and 9 following values
            if (i >= 9 && i + 9 < srCount)
            {
                //avgb9 = average of previous 9 values
                var avgb9 = (sRecords[i - 1].Distance + sRecords[i - 2].Distance + sRecords[i - 3].Distance + sRecords[i - 4].Distance + sRecords[i - 5].Distance + sRecords[i - 6].Distance + sRecords[i - 7].Distance + sRecords[i - 8].Distance + sRecords[i - 9].Distance) / 9;
                Console.WriteLine($"avgb9= {avgb9} avgb9-c= {avgb9 - record.Distance} c-avgb9={record.Distance - avgb9}");
                //avgb9c = absolute value of avgb9 - current value
                var avgb9c = Math.Abs(Convert.ToDecimal(avgb9 - record.Distance));
                //avga9 = average of following 9
                var avga9 = (sRecords[i + 1].Distance + sRecords[i + 2].Distance + sRecords[i + 3].Distance + sRecords[i + 4].Distance + sRecords[i + 5].Distance + sRecords[i + 6].Distance + sRecords[i + 7].Distance + sRecords[i + 8].Distance + sRecords[i + 9].Distance) / 9;
                Console.WriteLine($"avga9= {avga9} avga9-c= {avga9 - record.Distance} c-avga9={record.Distance - avga9}");
                //avga9c = absolute value of avga9 - current
                var avga9c = Math.Abs(Convert.ToDecimal(avga9 - record.Distance));
                
                //if both the avgb9c as well as the avga9c are bigger than or equal to 3x the IQR, flag as anomaly
                if ((avgb9c >= 3 * Convert.ToDecimal(IQR)) && (avga9c >= 3 * Convert.ToDecimal(IQR))) avgAnom = true;
            }

            //depending on their flags, view value as possible anomaly (1/2 flags) or certain anomaly (2/2) 
            if (avgAnom == true && ((record.Distance < (Q1 - 1.5 * IQR)) || (record.Distance > (Q3 + 1.5 * IQR))))
            {
                Console.WriteLine($"!ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (methode: avgAnom & IQR)");
            } else if ((record.Distance < (Q1 - 1.5 * IQR)) || (record.Distance > (Q3 + 1.5 * IQR)))
            {
                Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (method: IQR)");
            } else if (avgAnom == true)
            {
                Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (method: avgAnom)");
            }
        } else if ((record.Distance < (Q1 - 1.5 * IQR)) || (record.Distance > (Q3 + 1.5 * IQR)))
        { //check first and last value for IQR despite inability to perform avgAnom check
            Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (method: IQR)");
        }
        i++; //increment +1
    }
}
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();