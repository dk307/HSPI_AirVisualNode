using HomeSeerAPI;
using Hspi.Connector;
using NullGuard;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hspi.DeviceData
{
    using static System.FormattableString;

    /// <summary>
    ///  Base class for Child Devices
    /// </summary>
    /// <seealso cref="Hspi.DeviceDataBase" />
    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal abstract class DeviceData : DeviceDataBase
    {
        public DeviceData(DeviceType deviceType)
        {
            DeviceType = deviceType;
        }

        public override void SetInitialData(IHSApplication HS, int refId)
        {
            HS.SetDeviceValueByRef(refId, 0D, false);
            HS.set_DeviceInvalidValue(refId, true);
        }

        public abstract void Update(IHSApplication HS, double deviceValue, DateTime updateTime);

        public virtual Task HandleCommand(AirVisualNodeConnector connector, double value,
            ePairControlUse control, CancellationToken token) => throw new NotImplementedException();

        public int RefId { get; set; }
        public DeviceType DeviceType { get; }
        public override int HSDeviceType => 0;
        public override string HSDeviceTypeString => Invariant($"{PluginData.PlugInName} Information Device");
    };
}