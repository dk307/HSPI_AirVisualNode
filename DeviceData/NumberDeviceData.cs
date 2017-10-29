using HomeSeerAPI;
using System;
using System.Collections.Generic;

namespace Hspi.DeviceData
{
    internal class NumberDeviceData : DeviceData
    {
        public NumberDeviceData(DeviceType deviceType) : base(deviceType)
        {
        }

        public override void Update(IHSApplication HS, double value, DateTime updateTime)
        {
            UpdateDeviceData(HS, RefId, value, updateTime);
        }

        public override DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI DeviceAPI => DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Energy;

        public override IList<VSVGPairs.VSPair> StatusPairs
        {
            get
            {
                var pairs = new List<VSVGPairs.VSPair>();
                pairs.Add(new VSVGPairs.VSPair(HomeSeerAPI.ePairStatusControl.Status)
                {
                    PairType = VSVGPairs.VSVGPairType.Range,
                    RangeStart = int.MinValue,
                    RangeEnd = int.MaxValue,
                    IncludeValues = true,
                    RangeStatusDecimals = 1,
                    RangeStatusSuffix = " " + PluginConfig.GetUnits(DeviceType),
                });
                return pairs;
            }
        }

        public override IList<VSVGPairs.VGPair> GraphicsPairs => new List<VSVGPairs.VGPair>();
    }
}