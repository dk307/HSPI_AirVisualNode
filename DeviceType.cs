using System.ComponentModel;

namespace Hspi
{
    internal enum DeviceType
    {
        CO2 = 1,

        Humidity,

        [Description("PM25 AQI China")]
        PM25AQICN,

        [Description("PM25 AQI USA")]
        PM25AQI,

        [Description("Outside PM25 AQI China")]
        OutsidePM25AQICN,

        [Description("Outside PM25 AQI USA")]
        OutsidePM25AQI,

        [Description("PM10")]
        PM10,

        [Description("PM25")]
        PM25,

        [Description("Temperature(C)")]
        TemperatureC,

        [Description("Temperature(F)")]
        TemperatureF,
    }
}