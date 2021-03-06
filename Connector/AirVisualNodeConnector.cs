﻿using Hspi.Connector.Model;
using Hspi.Utils;
using SharpCifs.Smb;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

namespace Hspi.Connector
{
    internal sealed class AirVisualNodeConnector : IDisposable
    {
        static AirVisualNodeConnector()
        {
            //Turn off all caching and reuse
            SharpCifs.Config.SetProperty("jcifs.netbios.cachePolicy", "0");
            SharpCifs.Config.SetProperty("jcifs.smb.client.ssnLimit", "1");
            SharpCifs.Config.SetProperty("jcifs.smb.client.attrExpirationPeriod", "0");
            SharpCifs.Config.SetProperty("jcifs.smb.client.responseTimeout", "10000");
            SharpCifs.Config.SetProperty("jcifs.smb.client.soTimeout", "10000");
        }

        public AirVisualNodeConnector(IPAddress deviceIP,
                                      NetworkCredential credentials,
                                      CancellationToken token)
        {
            DeviceIP = deviceIP;
            this.credentials = credentials;
            sourceShutdownToken = CancellationTokenSource.CreateLinkedTokenSource(token);
        }

        public event EventHandler<SensorData> SensorDataChanged;

        public IPAddress DeviceIP { get; }

        public void Connect()
        {
            TaskHelper.StartAsyncWithErrorChecking(Invariant($"{DeviceIP} Connection"),
                                                   StartWorking,
                                                   sourceShutdownToken.Token);
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                sourceShutdownToken.Cancel();
                sourceShutdownToken.Dispose();
                disposedValue = true;
            }
        }

        private static void ClearSmbState()
        {
            try
            {
                SmbTransport.ClearCachedConnections(true);
                SmbFile.Initialize();
            }
            catch { }
        }

        private static double ParseValue(string[] values, int index)
        {
            var value = double.Parse(values[index], NumberStyles.Float, CultureInfo.InvariantCulture);
            return value;
        }

        private static long ParseLong(string[] values, int index)
        {
            return long.Parse(values[index], CultureInfo.InvariantCulture);
        }

        private async Task GetData()
        {
            try
            {
                Trace.WriteLine(Invariant($"Connecting to {DeviceIP}"));
                ClearSmbState();

                DateTime localTime = DateTime.Now.ToLocalTime();
                string path = Invariant($"smb://{DeviceIP}/airvisual/{localTime.Year}{localTime.Month:00}_AirVisual_values.txt");

                var auth = new NtlmPasswordAuthentication(null, credentials.UserName, credentials.Password);
                string lastString = null;
                var smbFile = new SmbFile(path, auth, SmbFile.FileShareRead | SmbFile.FileShareWrite);
                smbFile.Connect();

                using (Stream fileStream = smbFile.GetInputStream())
                {
                    var length = fileStream.Length;
                    Trace.WriteLine(Invariant($"Reading from {path} with size {length} Bytes"));

                    int bufferSize = 512;
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

                if (string.IsNullOrWhiteSpace(lastString))
                {
                    throw new IOException("Last String Read From file is empty");
                }

                Trace.WriteLine(Invariant($"Found data {lastString} from {path}"));

                var tokens = lastString.Split(';');

                SensorData sensorData = new SensorData
                {
                    //Date;Time;Timestamp;PM2_5(ug/m3);AQI(US);AQI(CN);PM10(ug/m3);Outdoor AQI(US);Outdoor AQI(CN);Temperature(C);Temperature(F);Humidity(%RH);CO2(ppm);VOC(ppb)
                    updateTime = new DateTime(DateTimeOffset.FromUnixTimeSeconds(ParseLong(tokens, 2)).Ticks)
                };

                if (lastUpdate != sensorData.updateTime)
                {
                    sensorData.PM25 = ParseValue(tokens, 3);
                    sensorData.PM25AQI = ParseValue(tokens, 4);
                    sensorData.PM25AQICN = ParseValue(tokens, 5);
                    sensorData.PM10 = ParseValue(tokens, 6);
                    sensorData.OutsidePM25AQI = ParseValue(tokens, 7);
                    sensorData.OutsidePM25AQICN = ParseValue(tokens, 8);
                    sensorData.TemperatureC = ParseValue(tokens, 9);
                    sensorData.TemperatureF = ParseValue(tokens, 10);
                    sensorData.Humidity = ParseValue(tokens, 11);
                    sensorData.CO2 = ParseValue(tokens, 12);

                    UpdateDelta(sensorData);
                    lastUpdate = sensorData.updateTime;
                    Trace.TraceInformation(Invariant($"Updated data for device {DeviceIP} for time {sensorData.updateTime}"));
                }
            }
            catch (Exception ex)
            {
                if (ex.IsCancelException())
                {
                    throw;
                }

                Trace.TraceError(Invariant($"Failed to get data from {DeviceIP}. {ex.GetFullMessage()}."));
            }
        }

        private async Task StartWorking()
        {
            while (!sourceShutdownToken.Token.IsCancellationRequested)
            {
                await GetData().ConfigureAwait(false);
                await Task.Delay(10000, sourceShutdownToken.Token).ConfigureAwait(false);
            }
        }

        private void UpdateDelta(SensorData data)
        {
            SensorDataChanged?.Invoke(this, data);
        }

        private readonly NetworkCredential credentials;
        private readonly CancellationTokenSource sourceShutdownToken;
        private bool disposedValue;
        private DateTime lastUpdate;
    }
}