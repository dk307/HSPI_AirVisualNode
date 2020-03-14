using HomeSeerAPI;
using Hspi.Connector.Model;
using Hspi.Exceptions;
using NullGuard;
using Scheduler.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.FormattableString;

namespace Hspi.DeviceData
{
    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal class DeviceRootDeviceManager
    {
        public DeviceRootDeviceManager(string deviceName, string rootDeviceId, IHSApplication HS)
        {
            this.deviceName = deviceName;
            this.HS = HS;
            this.rootDeviceId = rootDeviceId;
            GetCurrentDevices();
        }

        public void ProcessSensorData(SensorData sensorData)
        {
            UpdateSensorValue(DeviceType.CO2, sensorData.CO2, sensorData.updateTime);
            UpdateSensorValue(DeviceType.Humidity, sensorData.Humidity, sensorData.updateTime);
            UpdateSensorValue(DeviceType.OutsidePM25AQI, sensorData.OutsidePM25AQI, sensorData.updateTime);
            UpdateSensorValue(DeviceType.OutsidePM25AQICN, sensorData.OutsidePM25AQICN, sensorData.updateTime);
            UpdateSensorValue(DeviceType.PM10, sensorData.PM10, sensorData.updateTime);
            UpdateSensorValue(DeviceType.PM25, sensorData.PM25, sensorData.updateTime);
            UpdateSensorValue(DeviceType.PM25AQI, sensorData.PM25AQI, sensorData.updateTime);
            UpdateSensorValue(DeviceType.PM25AQICN, sensorData.PM25AQICN, sensorData.updateTime);
            UpdateSensorValue(DeviceType.TemperatureC, sensorData.TemperatureC, sensorData.updateTime);
            UpdateSensorValue(DeviceType.TemperatureF, sensorData.TemperatureF, sensorData.updateTime);
        }

        private void UpdateSensorValue(DeviceType deviceType, double value, DateTime updateTime)
        {
            var deviceIdentifier = new DeviceIdentifier(rootDeviceId, deviceType);

            string address = deviceIdentifier.Address;
            if (!currentChildDevices.ContainsKey(address))
            {
                CreateDevice(deviceIdentifier);
            }

            var childDevice = currentChildDevices[address];

            childDevice.Update(HS, value, updateTime);
        }

        private void GetCurrentDevices()
        {
            var deviceEnumerator = HS.GetDeviceEnumerator() as clsDeviceEnumeration;

            if (deviceEnumerator == null)
            {
                throw new HspiException(Invariant($"{PluginData.PlugInName} failed to get a device enumerator from HomeSeer."));
            }

            string parentAddress = DeviceIdentifier.CreateRootAddress(rootDeviceId);
            do
            {
                DeviceClass device = deviceEnumerator.GetNext();
                if ((device != null) &&
                    (device.get_Interface(HS) != null) &&
                    (device.get_Interface(HS).Trim() == PluginData.PlugInName))
                {
                    string address = device.get_Address(HS);
                    if (address == parentAddress)
                    {
                        parentRefId = device.get_Ref(HS);
                    }
                    else if (address.StartsWith(parentAddress, StringComparison.Ordinal))
                    {
                        DeviceData childDeviceData = GetDeviceData(device);
                        if (childDeviceData != null)
                        {
                            currentChildDevices.Add(address, childDeviceData);
                        }
                    }
                }
            } while (!deviceEnumerator.Finished);
        }

        private void CreateDevice(DeviceIdentifier deviceIdentifier)
        {
            if (!parentRefId.HasValue)
            {
                string parentAddress = deviceIdentifier.RootDeviceAddress;
                var parentHSDevice = CreateDevice(null, deviceName, parentAddress, new RootDeviceData());
                parentRefId = parentHSDevice.get_Ref(HS);
            }

            string address = deviceIdentifier.Address;
            var childDevice = GetDevice(deviceIdentifier.DeviceType);
            string childDeviceName = Invariant($"{deviceName} {EnumHelper.GetDescription(childDevice.DeviceType)}");
            var childHSDevice = CreateDevice(parentRefId.Value, childDeviceName, address, childDevice);
            childDevice.RefId = childHSDevice.get_Ref(HS);
            currentChildDevices[address] = childDevice;
        }

        private static DeviceData GetDevice(DeviceType deviceType)
        {
            return new NumberDeviceData(deviceType);
        }

        private DeviceData GetDeviceData(DeviceClass hsDevice)
        {
            var id = DeviceIdentifier.Identify(hsDevice);
            if (id == null)
            {
                return null;
            }

            var device = GetDevice(id.DeviceType);
            device.RefId = hsDevice.get_Ref(HS);
            return device;
        }

        /// <summary>
        /// Creates the HS device.
        /// </summary>
        /// <param name="optionalParentRefId">The optional parent reference identifier.</param>
        /// <param name="name">The name of device</param>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="deviceData">The device data.</param>
        /// <returns>
        /// New Device
        /// </returns>
        private DeviceClass CreateDevice(int? optionalParentRefId, string name, string deviceAddress, DeviceDataBase deviceData)
        {
            Trace.TraceInformation(Invariant($"Creating Device with Address:{deviceAddress}"));

            DeviceClass device = null;
            int refId = HS.NewDeviceRef(name);
            if (refId > 0)
            {
                device = (DeviceClass)HS.GetDeviceByRef(refId);
                string address = deviceAddress;
                device.set_Address(HS, address);
                device.set_Device_Type_String(HS, deviceData.HSDeviceTypeString);
                var deviceType = new DeviceTypeInfo_m.DeviceTypeInfo
                {
                    Device_API = deviceData.DeviceAPI,
                    Device_Type = deviceData.HSDeviceType
                };

                device.set_DeviceType_Set(HS, deviceType);
                device.set_Interface(HS, PluginData.PlugInName);
                device.set_InterfaceInstance(HS, string.Empty);
                device.set_Last_Change(HS, DateTime.Now);
                device.set_Location(HS, PluginData.PlugInName);

                device.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);
                device.MISC_Set(HS, Enums.dvMISC.STATUS_ONLY);
                device.MISC_Clear(HS, Enums.dvMISC.AUTO_VOICE_COMMAND);
                device.MISC_Clear(HS, Enums.dvMISC.SET_DOES_NOT_CHANGE_LAST_CHANGE);
                device.set_Status_Support(HS, false);

                var pairs = deviceData.StatusPairs;
                foreach (var pair in pairs)
                {
                    HS.DeviceVSP_AddPair(refId, pair);
                }

                var gPairs = deviceData.GraphicsPairs;
                foreach (var gpair in gPairs)
                {
                    HS.DeviceVGP_AddPair(refId, gpair);
                }

                DeviceClass parent = null;
                if (optionalParentRefId.HasValue)
                {
                    parent = (DeviceClass)HS.GetDeviceByRef(optionalParentRefId.Value);
                }

                if (parent != null)
                {
                    parent.set_Relationship(HS, Enums.eRelationship.Parent_Root);
                    device.set_Relationship(HS, Enums.eRelationship.Child);
                    device.AssociatedDevice_Add(HS, parent.get_Ref(HS));
                    parent.AssociatedDevice_Add(HS, device.get_Ref(HS));
                }

                deviceData.SetInitialData(HS, refId);
            }

            return device;
        }

        private readonly string rootDeviceId;
        private readonly string deviceName;
        private readonly IHSApplication HS;
        private int? parentRefId = null;
        private readonly IDictionary<string, DeviceData> currentChildDevices = new Dictionary<string, DeviceData>();
    };
}