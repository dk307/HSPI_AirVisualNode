﻿using HomeSeerAPI;
using NullGuard;
using System;
using System.Collections.Generic;
using System.IO;

namespace Hspi.DeviceData
{
    /// <summary>
    /// This is base class for creating and updating devices in HomeSeer.
    /// </summary>
    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal abstract class DeviceDataBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceDataBase" /> class.
        /// </summary>
        /// <param name="name">Name of the Device</param>
        protected DeviceDataBase()
        {
        }

        /// <summary>
        /// Gets the status pairs for creating device.
        /// </summary>
        /// <param name="config">The plugin configuration.</param>
        /// <returns></returns>
        public abstract IList<VSVGPairs.VSPair> StatusPairs { get; }

        /// <summary>
        /// Gets the graphics pairs for creating device
        /// </summary>
        /// <param name="config">The plugin configuration.</param>
        /// <returns></returns>
        public abstract IList<VSVGPairs.VGPair> GraphicsPairs { get; }

        public abstract int HSDeviceType { get; }
        public virtual DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI DeviceAPI => DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Plug_In;
        public abstract string HSDeviceTypeString { get; }

        /// <summary>
        /// Sets the initial data for the device.
        /// </summary>
        /// <param name="HS">The hs.</param>
        /// <param name="RefId">The reference identifier.</param>
        public abstract void SetInitialData(IHSApplication HS, int RefId);

        protected static IList<VSVGPairs.VGPair> GetSingleGraphicsPairs(string fileName)
        {
            var pairs = new List<VSVGPairs.VGPair>();
            pairs.Add(new VSVGPairs.VGPair()
            {
                PairType = VSVGPairs.VSVGPairType.Range,
                Graphic = Path.Combine(PluginData.HSImagesPathRoot, fileName),
                RangeStart = int.MinValue,
                RangeEnd = int.MaxValue,
            });
            return pairs;
        }

        /// <summary>
        /// Updates the device data from number data
        /// </summary>
        /// <param name="HS">Homeseer application.</param>
        /// <param name="refId">The reference identifier.</param>
        /// <param name="data">Number data.</param>
        protected static void UpdateDeviceData(IHSApplication HS, int refId, double data, DateTime updateTime)
        {
            HS.set_DeviceInvalidValue(refId, false);
            HS.SetDeviceValueByRef(refId, data, true);
            HS.SetDeviceLastChange(refId, updateTime);
        }
    };
}