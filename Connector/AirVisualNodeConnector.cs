﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Hspi.Connector.Model;
using System.Globalization;
using System.IO;
using System.Text;

namespace Hspi.Connector
{
    using static System.FormattableString;

    internal class AirVisualNodeConnector : IDisposable
    {
        public AirVisualNodeConnector(IPAddress deviceIP, NetworkCredential credentials, ILogger logger)
        {
            this.logger = logger;
            DeviceIP = deviceIP;
            this.credentials = credentials;
        }

        public event EventHandler<SensorData> SensorDataChanged;

        public IPAddress DeviceIP { get; }

        public void Connect(CancellationToken token)
        {
            startWorkingTask = Task.Factory.StartNew(async () => await StartWorking(token), token,
                                       TaskCreationOptions.LongRunning,
                                       TaskScheduler.Current).Unwrap();
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        private static float ParseFloat(string[] values, int index)
        {
            return float.Parse(values[index], CultureInfo.InvariantCulture);
        }

        private static long ParseLong(string[] values, int index)
        {
            return long.Parse(values[index], CultureInfo.InvariantCulture);
        }

        private async Task GetData()
        {
            try
            {
                logger.LogDebug(Invariant($"Connecting to {DeviceIP}"));

                string share = Invariant($"\\\\{DeviceIP}\\airvisual");
                using (NetworkConnection networkConnection = new NetworkConnection(share, credentials))
                {
                    string lastString = null;
                    DateTime localTime = DateTime.Now.ToLocalTime();
                    string path = Invariant($"{share}\\{localTime.Year}{localTime.Month}_AirVisual_values.txt");

                    logger.LogDebug(Invariant($"Reading from {path}"));

                    int bufferSize = 1024;
                    using (var fileStream = new FileStream(path,
                                                           FileMode.Open,
                                                           FileAccess.Read,
                                                           FileShare.ReadWrite | FileShare.Delete,
                                                           bufferSize))
                    {
                        fileStream.Seek(0, SeekOrigin.End);
                        fileStream.Seek(-Math.Min(bufferSize, fileStream.Length), SeekOrigin.Current);

                        using (var reader = new StreamReader(fileStream, Encoding.ASCII, false, bufferSize))
                        {
                            while (!reader.EndOfStream)
                            {
                                lastString = await reader.ReadLineAsync();
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(lastString))
                    {
                        throw new IOException("Last String Read From file is empty");
                    }

                    logger.LogDebug(Invariant($"Found data {lastString} from {path}"));

                    var tokens = lastString.Split(';');

                    SensorData sensorData = new SensorData();

                    //Date;Time;Timestamp;PM2_5(ug/m3);AQI(US);AQI(CN);PM10(ug/m3);Outdoor AQI(US);Outdoor AQI(CN);Temperature(C);Temperature(F);Humidity(%RH);CO2(ppm);VOC(ppb)
                    sensorData.updateTime = new DateTime(DateTimeOffset.FromUnixTimeSeconds(ParseLong(tokens, 2)).Ticks);

                    if (lastUpdate != sensorData.updateTime)
                    {
                        sensorData.PM25 = ParseFloat(tokens, 3);
                        sensorData.PM25AQI = ParseFloat(tokens, 4);
                        sensorData.PM25AQICN = ParseFloat(tokens, 5);
                        sensorData.PM10 = ParseFloat(tokens, 6);
                        sensorData.OutsidePM25AQI = ParseFloat(tokens, 7);
                        sensorData.OutsidePM25AQICN = ParseFloat(tokens, 8);
                        sensorData.TemperatureC = ParseFloat(tokens, 9);
                        sensorData.TemperatureF = ParseFloat(tokens, 10);
                        sensorData.Humidity = ParseFloat(tokens, 11);
                        sensorData.CO2 = ParseFloat(tokens, 12);

                        UpdateDelta(sensorData);
                        lastUpdate = sensorData.updateTime;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(Invariant($"Failed to  get data from {DeviceIP}. {ex.GetFullMessage()}."));
            }
        }

        private async Task StartWorking(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await GetData().ConfigureAwait(false);
                await Task.Delay(10000, token).ConfigureAwait(false);
            }
        }

        private void UpdateDelta(SensorData data)
        {
            SensorDataChanged?.Invoke(this, data);
        }

        private readonly NetworkCredential credentials;
        private readonly ILogger logger;
        private bool disposedValue = false;
        private object lastMessageLock = new object();
        private DateTime lastUpdate;
        private Task startWorkingTask;
        // To detect redundant calls
    }
}