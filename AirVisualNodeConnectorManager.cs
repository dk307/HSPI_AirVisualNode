using HomeSeerAPI;
using Hspi.Connector.Model;
using Hspi.DeviceData;
using Hspi.Exceptions;
using NullGuard;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hspi.Connector
{
    using System.Net;
    using static System.FormattableString;

    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal class AirVisualNodeConnectorManager : IDisposable
    {
        public AirVisualNodeConnectorManager(IHSApplication HS, AirVisualNode device, ILogger logger, CancellationToken shutdownToken)
        {
            this.HS = HS;
            this.logger = logger;
            this.Device = device;
            rootDeviceData = new DeviceRootDeviceManager(device.Name, device.Id, this.HS, logger);

            combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken, instanceCancellationSource.Token);
            connector = new AirVisualNodeConnector(Device.DeviceIP,
                                                           new NetworkCredential(Device.Username, Device.Password),
                                                           logger);
            connector.SensorDataChanged += SensorDataChanged;

            connector.Connect(Token);
            processTask = Task.Factory.StartNew(ProcessDeviceUpdates, Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();
        }

        public AirVisualNode Device { get; }

        private CancellationToken Token => combinedCancellationSource.Token;

        public void Cancel()
        {
            instanceCancellationSource.Cancel();
            processTask.Wait();
        }

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
                    instanceCancellationSource.Cancel();
                    instanceCancellationSource.Dispose();
                    combinedCancellationSource.Dispose();

                    processTask.Dispose();
                    changedSensorData.Dispose();
                    DisposeConnector();
                    rootDeviceDataLock.Dispose();
                }

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
                    if (changedSensorData.TryTake(out var sensorData, -1, Token))
                    {
                        await rootDeviceDataLock.WaitAsync(Token);
                        try
                        {
                            rootDeviceData.ProcessSensorData(Device, sensorData);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(Invariant($"Failed to update Sensor Data for {Device.DeviceIP} with {ex.Message}"));
                        }
                        finally
                        {
                            rootDeviceDataLock.Release();
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
                changedSensorData.Add(data, Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private readonly BlockingCollection<SensorData> changedSensorData = new BlockingCollection<SensorData>();
        private readonly CancellationTokenSource combinedCancellationSource;
        private readonly AirVisualNodeConnector connector;
        private readonly IHSApplication HS;
        private readonly CancellationTokenSource instanceCancellationSource = new CancellationTokenSource();
        private readonly ILogger logger;
        private readonly Task processTask;
        private readonly DeviceRootDeviceManager rootDeviceData;
        private readonly SemaphoreSlim rootDeviceDataLock = new SemaphoreSlim(1);
        private bool disposedValue = false; // To detect redundant calls
    }
}