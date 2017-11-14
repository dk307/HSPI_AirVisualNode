using Hspi.Connector.Model;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpCifs.Smb;

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
            Task.Factory.StartNew(async () => await StartWorking(token), token,
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

                DateTime localTime = DateTime.Now.ToLocalTime();
                string path = Invariant($"smb://{DeviceIP}/airvisual/{localTime.Year}{localTime.Month}_AirVisual_values.txt");

                var auth = new NtlmPasswordAuthentication(null, credentials.UserName, credentials.Password);
                string lastString = null;
                using (var smbFile = new SmbFile(path, auth, SmbFile.FileShareRead | SmbFile.FileShareWrite))
                {
                    smbFile.Connect();

                    using (Stream fileStream = smbFile.GetInputStream())
                    {
                        var length = fileStream.Length;
                        logger.LogDebug(Invariant($"Reading from {path} with size {length} Bytes"));

                        int bufferSize = 16 * 1024;
                        fileStream.Seek(-Math.Min(bufferSize, length), SeekOrigin.End);

                        var pos = fileStream.Position;

                        using (var reader = new StreamReader(fileStream, Encoding.ASCII, false, bufferSize))
                        {
                            string lastData = await reader.ReadToEndAsync().ConfigureAwait(false);

                            foreach (var reading in lastData.Split('\n'))
                            {
                                if (!string.IsNullOrWhiteSpace(reading))
                                {
                                    lastString = reading;
                                }
                            }
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
                    logger.LogInfo(Invariant($"Updated data for device {DeviceIP} for time {sensorData.updateTime}"));
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
        private DateTime lastUpdate;
    }
}