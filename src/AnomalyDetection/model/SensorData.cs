using System;

namespace AnomalyDetection.Model
{
    public class SensorData
    {
        public string? name { get; set; }
        public string? tags { get; set; }
        public DateTime time { get; set; }
        public double? BatV { get; set; }
        public int? RSSI { get; set; }
        public double? batv { get; set; }
        public int? distance { get; set; }
        public int? distance_raw { get; set; }
        public string? level { get; set; }
        public string? liter { get; set; }
    }
}



