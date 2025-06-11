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

//group by sensor code iban
var groupedBySensor = records
    .GroupBy(r => r.Tags)
    .ToList();

foreach (var group in groupedBySensor)
{
    var sensorId = group.Key;
    var sRecords = group.OrderBy(r => r.Time).ToList(); //sRecords = sorted records
    
    //naam sensor huidige waarden
    Console.WriteLine($"\n Sensor: {sensorId}");

    //IQR: kwartielen en interkwartielbereik berekenen
    List<double> distances = sRecords.Select(r => (double)r.Distance.GetValueOrDefault()).ToList();
    distances.Sort();
    double Q1 = QuartileVM.GetPercentile(distances, 25);
    double Q3 = QuartileVM.GetPercentile(distances, 75);
    double IQR = Q3 - Q1;
    Console.WriteLine($"First quartile is: {Q1} \r\nThird quartile is: {Q3} \r\nIQR is: {IQR} \r\nSo we check for: {Q1 - 1.5 * IQR} or {Q3 + 1.5 * IQR}");
    
    //lengte lijst sensorwaarden en increment integer voor foreach instellen
    var srCount = sRecords.Count();
    int i = 0;

    //herken en print de anomalies: records van huidige sensor aflopen
    foreach (var record in sRecords)
    {
        //bool om bij te houden of mijn bewerking de huidige waarde als anomalie flagt
        bool avgAnom = false; //avgAnom = anomalie a.d.h.v. gemiddelde

        //de eerste en laatste waarde kunnen niet vergeleken worden met zowel vorige als volgende waarde
        if (record != sRecords[0] && i + 1 < srCount)
        {
            //c= current (huidige waarde); b = before (vorige waarde); a = after (volgende waarde)
            var ba = (sRecords[i - 1].Distance + sRecords[i + 1].Distance) / 2;
            var bc = (sRecords[i - 1].Distance + record.Distance) / 2;
            var ca = (sRecords[i + 1].Distance + record.Distance) / 2;
            //gemiddelde van ba, bc en ca berekend om te vergelijken en eventueel te gebruiken (momenteel niet in gebruik)
            Console.WriteLine($"c= {record.Distance} ba= {ba} bc= {bc} ca= {ca}");

            //scope vergroot: waarden moeten 9 vorige en volgende waarden hebben
            if (i >= 9 && i + 9 < srCount)
            {
                //avgb9 = gemiddelde vorige 9 waarden
                var avgb9 = (sRecords[i - 1].Distance + sRecords[i - 2].Distance + sRecords[i - 3].Distance + sRecords[i - 4].Distance + sRecords[i - 5].Distance + sRecords[i - 6].Distance + sRecords[i - 7].Distance + sRecords[i - 8].Distance + sRecords[i - 9].Distance) / 9;
                Console.WriteLine($"avgb9= {avgb9} avgb9-c= {avgb9 - record.Distance} c-avgb9={record.Distance - avgb9}");
                //avgb9c = absolute waarde van avgb9 - huidige waarde
                var avgb9c = Math.Abs(Convert.ToDecimal(avgb9 - record.Distance));
                //avga9 = gemiddelde volgende 9
                var avga9 = (sRecords[i + 1].Distance + sRecords[i + 2].Distance + sRecords[i + 3].Distance + sRecords[i + 4].Distance + sRecords[i + 5].Distance + sRecords[i + 6].Distance + sRecords[i + 7].Distance + sRecords[i + 8].Distance + sRecords[i + 9].Distance) / 9;
                Console.WriteLine($"avga9= {avga9} avga9-c= {avga9 - record.Distance} c-avga9={record.Distance - avga9}");
                //avga9c = absolute waarde van avga9 - huidig
                var avga9c = Math.Abs(Convert.ToDecimal(avga9 - record.Distance));
                
                //als zowel de avgb9c als de avga9c groter dan of gelijk aan 3x de IQR zijn, als anomalie flaggen
                if ((avgb9c >= 3 * Convert.ToDecimal(IQR)) && (avga9c >= 3 * Convert.ToDecimal(IQR))) avgAnom = true;
            }

            //afhankelijk van welke flags ze hebben de waarden als mogelijke (1/2) of gegarandeerde anomalie (2/2) weergeven
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
        { //eerste en laatste waarde checken voor IQR ondanks gebrek aan avgAnom check
            Console.WriteLine($"!POSSIBLE ANOMALY! Tag = {sensorId}, Time= {record.Time}, Distance = {record.Distance} mm (method: IQR)");
        }
        i++; //increment +1
    }
}
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();