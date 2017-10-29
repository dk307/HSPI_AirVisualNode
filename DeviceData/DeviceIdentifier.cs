using Scheduler.Classes;
using System;

namespace Hspi.DeviceData
{
    using static System.FormattableString;

    internal class DeviceIdentifier
    {
        public DeviceIdentifier(string deviceId, DeviceType deviceType)
        {
            DeviceId = deviceId;
            DeviceType = deviceType;
        }

        public string DeviceId { get; }
        public DeviceType DeviceType { get; }

        public static DeviceIdentifier Identify(DeviceClass hsDevice)
        {
            var childAddress = hsDevice.get_Address(null);

            var parts = childAddress.Split(AddressSeparator);

            if (parts.Length != 3)
            {
                return null;
            }

            if (!Enum.TryParse(parts[2], out DeviceType deviceType))
            {
                return null;
            }

            return new DeviceIdentifier(parts[1], deviceType);
        }

        public static string CreateRootAddress(string deviceId) => Invariant($"{PluginData.PlugInName}{AddressSeparator}{deviceId}");

        public string RootDeviceAddress => CreateRootAddress(DeviceId);
        public string Address => Invariant($"{RootDeviceAddress}{AddressSeparator}{DeviceType}");

        private const char AddressSeparator = '.';
    }
}