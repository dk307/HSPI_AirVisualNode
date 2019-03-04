using HomeSeerAPI;
using Hspi.Connector.Model;
using Hspi.DeviceData;
using Nito.AsyncEx;
using NullGuard;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hspi.Connector
{
    using Hspi.Utils;
    using System.Diagnostics;
    using System.Net;
    using static System.FormattableString;

    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal sealed class AirVisualNodeConnectorManager : IDisposable
    {
        public AirVisualNodeConnectorManager(IHSApplication HS, AirVisualNode device, CancellationToken shutdownToken)
        {
            this.HS = HS;
            Device = device;
            rootDeviceData = new DeviceRootDeviceManager(device.Name, device.Id, this.HS);

            combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
            connector = new AirVisualNodeConnector(Device.DeviceIP,
                                                   new NetworkCredential(Device.Username, Device.Password),
                                                   combinedCancellationSource.Token);
            connector.SensorDataChanged += SensorDataChanged;

            connector.Connect();
            TaskHelper.StartAsyncWithErrorChecking("Node Updates", ProcessDeviceUpdates, Token);
        }

        public AirVisualNode Device { get; }

        private CancellationToken Token => combinedCancellationSource.Token;

        public void Dispose()
        {
            if (!disposedValue)
            {
                combinedCancellationSource.Cancel();

                DisposeConnector();
                combinedCancellationSource.Dispose();

                disposedValue = true;
            }
        }

        private void DisposeConnector()
        {
            if (connector != null)
            {
                connector.SensorDataChanged -= SensorDataChanged;
                connector.Dispose();
            }
        }

        private async Task ProcessDeviceUpdates()
        {
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    var sensorData = await changedSensorData.DequeueAsync(Token).ConfigureAwait(false);
                    using (var sync = await rootDeviceDataLock.LockAsync(Token).ConfigureAwait(false))
                    {
                        try
                        {
                            rootDeviceData.ProcessSensorData(Device, sensorData);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning(Invariant($"Failed to update Sensor Data for {Device.DeviceIP} with {ex.Message}"));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            { }
        }

        private void SensorDataChanged(object sender, SensorData data)
        {
            try
            {
                changedSensorData.Enqueue(data, Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private readonly AsyncProducerConsumerQueue<SensorData> changedSensorData = new AsyncProducerConsumerQueue<SensorData>();
        private readonly CancellationTokenSource combinedCancellationSource;
        private readonly AirVisualNodeConnector connector;
        private readonly IHSApplication HS;
        private readonly DeviceRootDeviceManager rootDeviceData;
        private readonly AsyncLock rootDeviceDataLock = new AsyncLock();
        private bool disposedValue = false; // To detect redundant calls
    }
}