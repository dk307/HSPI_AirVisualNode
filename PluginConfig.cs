using HomeSeerAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace Hspi
{
    using static System.FormattableString;

    /// <summary>
    /// Class to store PlugIn Configuration
    /// </summary>
    /// <seealso cref="System.IDisposable"/>
    internal class PluginConfig : IDisposable
    {
        public event EventHandler<EventArgs> ConfigChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfig"/> class.
        /// </summary>
        /// <param name="HS">The homeseer application.</param>
        public PluginConfig(IHSApplication HS)
        {
            this.HS = HS;

            debugLogging = GetValue(DebugLoggingKey, false);
            string deviceIdsConcatString = GetValue(DeviceIds, string.Empty);
            var deviceIds = deviceIdsConcatString.Split(DeviceIdsSeparator);

            foreach (var deviceId in deviceIds)
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    continue;
                }
                string ipAddressString = GetValue(IPAddressKey, string.Empty, deviceId);
                if (!IPAddress.TryParse(ipAddressString, out var deviceIP))
                {
                    deviceIP = IPAddress.None;
                }

                string name = GetValue(NameKey, string.Empty, deviceId);
                string username = GetValue(UserNameKey, string.Empty, deviceId);
                string passwordEncrypted = GetValue(PasswordKey, string.Empty, deviceId);
                string password = HS.DecryptString(passwordEncrypted, EncryptPassword);

                devices.Add(deviceId, new AirVisualNode(deviceId, name, deviceIP, username, password));
            }
        }

        /// <summary>
        /// Gets or sets the devices
        /// </summary>
        /// <value>
        /// The API key.
        /// </value>
        public IReadOnlyDictionary<string, AirVisualNode> Devices
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return new Dictionary<string, AirVisualNode>(devices);
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether debug logging is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [debug logging]; otherwise, <c>false</c>.
        /// </value>
        public bool DebugLogging
        {
            get
            {
                configLock.EnterReadLock();
                try
                {
                    return debugLogging;
                }
                finally
                {
                    configLock.ExitReadLock();
                }
            }

            set
            {
                configLock.EnterWriteLock();
                try
                {
                    SetValue(DebugLoggingKey, value);
                    debugLogging = value;
                }
                finally
                {
                    configLock.ExitWriteLock();
                }
            }
        }

        public void AddDevice(AirVisualNode device)
        {
            configLock.EnterWriteLock();
            try
            {
                devices[device.Id] = device;

                SetValue(NameKey, device.Name, device.Id);
                SetValue(IPAddressKey, device.DeviceIP.ToString(), device.Id);
                SetValue(UserNameKey, device.Username, device.Id);
                SetValue(PasswordKey, HS.EncryptString(device.Password, EncryptPassword), device.Id);

                SetValue(DeviceIds, devices.Keys.Aggregate((x, y) => x + DeviceIdsSeparator + y));
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        public void RemoveDevice(string deviceId)
        {
            configLock.EnterWriteLock();
            try
            {
                devices.Remove(deviceId);
                if (devices.Count > 0)
                {
                    SetValue(DeviceIds, devices.Keys.Aggregate((x, y) => x + DeviceIdsSeparator + y));
                }
                else
                {
                    SetValue(DeviceIds, string.Empty);
                }
                HS.ClearINISection(deviceId, FileName);
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        private T GetValue<T>(string key, T defaultValue)
        {
            return GetValue(key, defaultValue, DefaultSection);
        }

        private T GetValue<T>(string key, T defaultValue, string section)
        {
            string stringValue = HS.GetINISetting(section, key, null, FileName);

            if (stringValue != null)
            {
                try
                {
                    T result = (T)System.Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);
                    return result;
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private void SetValue<T>(string key, T value)
        {
            SetValue<T>(key, value, DefaultSection);
        }

        private void SetValue<T>(string key, T value, string section)
        {
            string stringValue = System.Convert.ToString(value, CultureInfo.InvariantCulture);
            HS.SaveINISetting(section, key, stringValue, FileName);
        }

        /// <summary>
        /// Fires event that configuration changed.
        /// </summary>
        public void FireConfigChanged()
        {
            if (ConfigChanged != null)
            {
                var ConfigChangedCopy = ConfigChanged;
                ConfigChangedCopy(this, EventArgs.Empty);
            }
        }

        public static string GetUnits(DeviceType deviceType)
        {
            switch (deviceType)
            {
                case DeviceType.CO2:
                    return "PPM";

                case DeviceType.Humidity:
                    return "%";

                case DeviceType.PM25AQICN:
                    return string.Empty;

                case DeviceType.PM25AQI:
                    return string.Empty;

                case DeviceType.OutsidePM25AQICN:
                    return string.Empty;

                case DeviceType.OutsidePM25AQI:
                    return string.Empty;

                case DeviceType.PM10:
                    return "ug/m3";

                case DeviceType.PM25:
                    return "ug/m3";

                case DeviceType.TemperatureC:
                    return "C";

                case DeviceType.TemperatureF:
                    return "F";

                default:
                    return string.Empty;
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    configLock.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion IDisposable Support

        private const string NameKey = "Name";
        private const string UserNameKey = "Username";
        private const string PasswordKey = "Password";
        private const string DeviceIds = "DevicesIds";
        private const string DebugLoggingKey = "DebugLogging";
        private readonly static string FileName = Invariant($"{Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location)}.ini");
        private const string IPAddressKey = "IPAddress";
        private const string DefaultSection = "Settings";
        private const string EncryptPassword = "Not sure what is more secure";
        private const char DeviceIdsSeparator = '|';

        private readonly Dictionary<string, AirVisualNode> devices = new Dictionary<string, AirVisualNode>();
        private readonly IHSApplication HS;
        private bool debugLogging;
        private bool disposedValue;
        private readonly ReaderWriterLockSlim configLock = new ReaderWriterLockSlim();
    };
}