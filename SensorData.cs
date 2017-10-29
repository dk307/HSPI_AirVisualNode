using System;
using System.Runtime.Serialization;

#pragma warning disable 0649

namespace Hspi.Connector.Model
{
    internal class SensorData
    {
        public DateTime updateTime;

        public double CO2;

        public double Humidity;

        public double PM25AQICN;

        public double PM25AQI;

        public double OutsidePM25AQICN;

        public double OutsidePM25AQI;

        public double PM10;

        public double PM25;

        public double TemperatureC;

        public double TemperatureF;
    }
}