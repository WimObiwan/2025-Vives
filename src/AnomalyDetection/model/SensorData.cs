using System;
using CsvHelper.Configuration.Attributes;

namespace AnomalyDetection.Model
{
    public class SensorData
    {
        [Name("name")]
        public string? Name { get; set; }

        [Name("tags")]
        public string? Tags { get; set; }

        [Name("time")]
        public DateTime Time { get; set; }

        [Name("BatV")]
        public double? BatteryRaw { get; set; }

        [Name("RSSI")]
        public int? RSSI { get; set; }

        [Name("batV")]
        public double? BatteryProcessed { get; set; }

        [Name("distance")]
        public int? Distance { get; set; }

        [Name("distance_raw")]
        public int? DistanceRaw { get; set; }

        [Name("level")]
        public string? Level { get; set; }

        [Name("liter")]
        public string? Liter { get; set; }

        // new record for the new distances without anomalies
        [Name("fixedDistance")]
        public int? FixedDistance { get; set; }
    }
}



