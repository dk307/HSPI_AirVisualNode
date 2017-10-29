using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Hspi.Connector
{
    internal sealed class NetworkConnection : IDisposable
    {
        public NetworkConnection(string networkName, NetworkCredential credentials)
        {
            this.networkName = networkName;

            var netResource = new NetResource()
            {
                Scope = ResourceScope.RESOURCE_GLOBALNET,
                Type = ResourceType.RESOURCETYPE_DISK,
                DisplayType = ResourceDisplayType.RESOURCEDISPLAYTYPE_SHARE,
                RemoteName = networkName
            };

            Disconnect();

            var result = WNetAddConnection2(
                netResource,
                credentials.Password,
                credentials.UserName,
                (int)ConnectionFlags.CONNECT_TEMPORARY);

            if (result != 0)
            {
                throw new Win32Exception(result);
            }
        }

        ~NetworkConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            Disconnect();
        }

        private void Disconnect()
        {
            WNetCancelConnection2(networkName, 0, true);
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        [Flags]
        private enum ConnectionFlags
        {
            CONNECT_TEMPORARY = 0x04,
        }

        public enum ResourceScope
        {
            RESOURCE_CONNECTED = 1,
            RESOURCE_GLOBALNET,
            RESOURCE_REMEMBERED,
            RESOURCE_RECENT,
            RESOURCE_CONTEXT
        };

        public enum ResourceType
        {
            RESOURCETYPE_ANY = 0,
            RESOURCETYPE_DISK = 1,
            RESOURCETYPE_PRINT = 2,
            RESOURCETYPE_RESERVED = 8,
            RESOURCETYPE_UNKNOWN = -1,
        };

        [Flags]
        public enum ResourceUsage
        {
            RESOURCEUSAGE_CONNECTABLE = 0x00000001,
            RESOURCEUSAGE_CONTAINER = 0x00000002,
            RESOURCEUSAGE_NOLOCALDEVICE = 0x00000004,
            RESOURCEUSAGE_SIBLING = 0x00000008,
            RESOURCEUSAGE_ATTACHED = 0x00000010,
            RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE | RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED),
        };

        public enum ResourceDisplayType
        {
            RESOURCEDISPLAYTYPE_GENERIC,
            RESOURCEDISPLAYTYPE_DOMAIN,
            RESOURCEDISPLAYTYPE_SERVER,
            RESOURCEDISPLAYTYPE_SHARE,
            RESOURCEDISPLAYTYPE_FILE,
            RESOURCEDISPLAYTYPE_GROUP,
            RESOURCEDISPLAYTYPE_NETWORK,
            RESOURCEDISPLAYTYPE_ROOT,
            RESOURCEDISPLAYTYPE_SHAREADMIN,
            RESOURCEDISPLAYTYPE_DIRECTORY,
            RESOURCEDISPLAYTYPE_TREE,
            RESOURCEDISPLAYTYPE_NDSCONTAINER
        };

        [StructLayout(LayoutKind.Sequential)]
        private class NetResource
        {
            public ResourceScope Scope = 0;
            public ResourceType Type = 0;
            public ResourceDisplayType DisplayType = 0;
            public ResourceUsage Usage = 0;
            public string LocalName = null;
            public string RemoteName = null;
            public string Comment = null;
            public string Provider = null;
        };

        private string networkName;
    }
}