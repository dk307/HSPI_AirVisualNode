// This code is derived from jcifs smb client library <jcifs at samba dot org>
// Ported by J. Arturo <webmaster at komodosoft dot net>
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
using System;
using System.Linq;
using System.Net;
using SharpCifs.Util;
using SharpCifs.Util.DbsHelper;
using SharpCifs.Util.Sharpen;
using Extensions = SharpCifs.Util.Sharpen.Extensions;

namespace SharpCifs.Netbios
{
    /// <summary>This class represents a NetBIOS over TCP/IP address.</summary>
    /// <remarks>
    /// This class represents a NetBIOS over TCP/IP address. Under normal
    /// conditions, users of jCIFS need not be concerned with this class as
    /// name resolution and session services are handled internally by the smb package.
    /// <p> Applications can use the methods <code>getLocalHost</code>,
    /// <code>getByName</code>, and
    /// <code>getAllByAddress</code> to create a new NbtAddress instance. This
    /// class is symmetric with
    /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
    /// .
    /// <p><b>About NetBIOS:</b> The NetBIOS name
    /// service is a dynamic distributed service that allows hosts to resolve
    /// names by broadcasting a query, directing queries to a server such as
    /// Samba or WINS. NetBIOS is currently the primary networking layer for
    /// providing name service, datagram service, and session service to the
    /// Microsoft Windows platform. A NetBIOS name can be 15 characters long
    /// and hosts usually registers several names on the network. From a
    /// Windows command prompt you can see
    /// what names a host registers with the nbtstat command.
    /// <p><blockquote><pre>
    /// C:\&gt;nbtstat -a 192.168.1.15
    /// NetBIOS Remote Machine Name Table
    /// Name               Type         Status
    /// ---------------------------------------------
    /// JMORRIS2        <00>  UNIQUE      Registered
    /// BILLING-NY      <00>  GROUP       Registered
    /// JMORRIS2        <03>  UNIQUE      Registered
    /// JMORRIS2        <20>  UNIQUE      Registered
    /// BILLING-NY      <1E>  GROUP       Registered
    /// JMORRIS         <03>  UNIQUE      Registered
    /// MAC Address = 00-B0-34-21-FA-3B
    /// </blockquote></pre>
    /// <p> The hostname of this machine is <code>JMORRIS2</code>. It is
    /// a member of the group(a.k.a workgroup and domain) <code>BILLING-NY</code>. To
    /// obtain an
    /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
    /// for a host one might do:
    /// <pre>
    /// InetAddress addr = NbtAddress.getByName( "jmorris2" ).getInetAddress();
    /// </pre>
    /// <p>From a UNIX platform with Samba installed you can perform similar
    /// diagnostics using the <code>nmblookup</code> utility.
    /// </remarks>
    /// <author>Michael B. Allen</author>
    /// <seealso cref="System.Net.IPAddress">System.Net.IPAddress</seealso>
    /// <since>jcifs-0.1</since>
    internal sealed class NbtAddress
    {
        /// <summary>A B node only broadcasts name queries.</summary>
        /// <remarks>
        /// A B node only broadcasts name queries. This is the default if a
        /// nameserver such as WINS or Samba is not specified.
        /// </remarks>
        public const int BNode = 0;

        /// <summary>
        /// A Point-to-Point node, or P node, unicasts queries to a nameserver
        /// only.
        /// </summary>
        /// <remarks>
        /// A Point-to-Point node, or P node, unicasts queries to a nameserver
        /// only. Natrually the <code>jcifs.netbios.nameserver</code> property must
        /// be set.
        /// </remarks>
        public const int PNode = 1;

        /// <summary>
        /// Try Broadcast queries first, then try to resolve the name using the
        /// nameserver.
        /// </summary>
        /// <remarks>
        /// Try Broadcast queries first, then try to resolve the name using the
        /// nameserver.
        /// </remarks>
        public const int MNode = 2;

        /// <summary>A Hybrid node tries to resolve a name using the nameserver first.</summary>
        /// <remarks>
        /// A Hybrid node tries to resolve a name using the nameserver first. If
        /// that fails use the broadcast address. This is the default if a nameserver
        /// is provided. This is the behavior of Microsoft Windows machines.
        /// </remarks>
        public const int HNode = 3;

        internal static readonly Name UnknownName = new Name("0.0.0.0", unchecked(0x00), null);

        internal static readonly NbtAddress UnknownAddress
            = new NbtAddress(UnknownName, 0, false, BNode);

        internal static readonly byte[] UnknownMacAddress =
        {
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00))
        };

        private static NbtAddress Localhost;

        static NbtAddress()
        {
            IPAddress localInetAddress;
            string localHostname;
            Name localName;
            localInetAddress = Extensions.GetLocalAddresses().FirstOrDefault();
            if (localInetAddress == null)
            {
                try
                {
                    localInetAddress = Extensions.GetAddressByName("127.0.0.1");
                }
                catch (UnknownHostException)
                {
                }
            }
            localHostname = Config.GetProperty("jcifs.netbios.hostname", null);
            if (string.IsNullOrEmpty(localHostname))
            {
                try
                {
                    localHostname = Dns.GetHostName();
                }
                catch (Exception)
                {
                    localHostname = "JCIFS_127_0_0_1";
                }
            }
            localName = new Name(localHostname,
                                 unchecked(0x00),
                                 Config.GetProperty("jcifs.netbios.scope", null));
            Localhost = new NbtAddress(localName,
                                       localInetAddress.GetHashCode(),
                                       false,
                                       BNode,
                                       false,
                                       false,
                                       true,
                                       false,
                                       UnknownMacAddress);
        }

        /// <summary>Retrieves the local host address.</summary>
        /// <remarks>Retrieves the local host address.</remarks>
        /// <exception cref="UnknownHostException">
        /// This is not likely as the IP returned
        /// by <code>InetAddress</code> should be available
        /// </exception>
        public static NbtAddress GetLocalHost()
        {
            return Localhost;
        }

        public static Name GetLocalName()
        {
            return Localhost.HostName;
        }

        /// <summary>Determines the address of a host given it's host name.</summary>
        /// <remarks>
        /// Determines the address of a host given it's host name. The name can be a NetBIOS name like
        /// "freto" or an IP address like "192.168.1.15". It cannot be a DNS name;
        /// the analygous
        /// <see cref="SharpCifs.UniAddress">Jcifs.UniAddress</see>
        /// or
        /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
        /// <code>getByName</code> methods can be used for that.
        /// </remarks>
        /// <param name="host">hostname to resolve</param>
        /// <exception cref="UnknownHostException">if there is an error resolving the name
        /// </exception>
        public static NbtAddress GetByName(string host)
        {
            return GetByName(host, unchecked(0x00), null);
        }

        /// <summary>Determines the address of a host given it's host name.</summary>
        /// <remarks>
        /// Determines the address of a host given it's host name. NetBIOS
        /// names also have a <code>type</code>. Types(aka Hex Codes)
        /// are used to distiquish the various services on a host. &lt;a
        /// href="../../../nbtcodes.html"&gt;Here</a> is
        /// a fairly complete list of NetBIOS hex codes. Scope is not used but is
        /// still functional in other NetBIOS products and so for completeness it has been
        /// implemented. A <code>scope</code> of <code>null</code> or <code>""</code>
        /// signifies no scope.
        /// </remarks>
        /// <param name="host">the name to resolve</param>
        /// <param name="type">the hex code of the name</param>
        /// <param name="scope">the scope of the name</param>
        /// <exception cref="UnknownHostException">if there is an error resolving the name
        /// </exception>
        public static NbtAddress GetByName(string host, int type, string scope)
        {
            return GetByName(host, type, scope, null);
        }

        /// <exception cref="UnknownHostException"></exception>
        public static NbtAddress GetByName(string host, int type, string scope, IPAddress svr)
        {
            int ip = unchecked(0x00);
            int hitDots = 0;
            char[] data = host.ToCharArray();
            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];

                int b = unchecked(0x00);
                while (c != '.')
                {
                    b = b * 10 + c - '0';
                    if (++i >= data.Length)
                    {
                        break;
                    }
                    c = data[i];
                }

                ip = (ip << 8) + b;
                hitDots++;
            }

            return new NbtAddress(UnknownName, ip, false, BNode);
        }

        internal Name HostName;

        internal int Address;

        internal int NodeType;

        internal bool GroupName;

        internal bool isBeingDeleted;

        internal bool isInConflict;

        internal bool isActive;

        internal bool isPermanent;

        internal bool IsDataFromNodeStatus;

        internal byte[] MacAddress;

        internal NbtAddress(Name hostName, int address, bool groupName, int nodeType)
        {
            this.HostName = hostName;
            this.Address = address;
            this.GroupName = groupName;
            this.NodeType = nodeType;
        }

        internal NbtAddress(Name hostName,
                            int address,
                            bool groupName,
                            int nodeType,
                            bool isBeingDeleted,
                            bool isInConflict,
                            bool isActive,
                            bool isPermanent,
                            byte[] macAddress)
        {
            this.HostName = hostName;
            this.Address = address;
            this.GroupName = groupName;
            this.NodeType = nodeType;
            this.isBeingDeleted = isBeingDeleted;
            this.isInConflict = isInConflict;
            this.isActive = isActive;
            this.isPermanent = isPermanent;
            this.MacAddress = macAddress;
            IsDataFromNodeStatus = true;
        }

        /// <summary>The hostname of this address.</summary>
        /// <remarks>
        /// The hostname of this address. If the hostname is null the local machines
        /// IP address is returned.
        /// </remarks>
        /// <returns>the text representation of the hostname associated with this address</returns>
        public string GetHostName()
        {
            if (HostName == UnknownName)
            {
                return GetHostAddress();
            }
            return HostName.name;
        }

        /// <summary>Returns the raw IP address of this NbtAddress.</summary>
        /// <remarks>
        /// Returns the raw IP address of this NbtAddress. The result is in network
        /// byte order: the highest order byte of the address is in getAddress()[0].
        /// </remarks>
        /// <returns>a four byte array</returns>
        public byte[] GetAddress()
        {
            byte[] addr = new byte[4];
            addr[0] = unchecked((byte)(((int)(((uint)Address) >> 24)) & unchecked(0xFF)));
            addr[1] = unchecked((byte)(((int)(((uint)Address) >> 16)) & unchecked(0xFF)));
            addr[2] = unchecked((byte)(((int)(((uint)Address) >> 8)) & unchecked(0xFF)));
            addr[3] = unchecked((byte)(Address & unchecked(0xFF)));
            return addr;
        }

        /// <summary>To convert this address to an <code>InetAddress</code>.</summary>
        /// <remarks>To convert this address to an <code>InetAddress</code>.</remarks>
        /// <returns>
        /// the
        /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
        /// representation of this address.
        /// </returns>
        /// <exception cref="UnknownHostException"></exception>
        public IPAddress GetInetAddress()
        {
            return Extensions.GetAddressByName(GetHostAddress());
        }

        /// <summary>
        /// Returns this IP adress as a
        /// <see cref="string">string</see>
        /// in the form "%d.%d.%d.%d".
        /// </summary>
        public string GetHostAddress()
        {
            return (((int)(((uint)Address) >> 24)) & unchecked(0xFF))
                    + "." + (((int)(((uint)Address) >> 16)) & unchecked(0xFF))
                    + "." + (((int)(((uint)Address) >> 8)) & unchecked(0xFF))
                    + "." + (((int)(((uint)Address) >> 0)) & unchecked(0xFF));
        }

        /// <summary>Returned the hex code associated with this name(e.g.</summary>
        /// <remarks>Returned the hex code associated with this name(e.g. 0x20 is for the file service)
        /// </remarks>
        public int GetNameType()
        {
            return HostName.HexCode;
        }

        /// <summary>Returns a hashcode for this IP address.</summary>
        /// <remarks>
        /// Returns a hashcode for this IP address. The hashcode comes from the IP address
        /// and is not generated from the string representation. So because NetBIOS nodes
        /// can have many names, all names associated with an IP will have the same
        /// hashcode.
        /// </remarks>
        public override int GetHashCode()
        {
            return Address;
        }

        /// <summary>Determines if this address is equal two another.</summary>
        /// <remarks>
        /// Determines if this address is equal two another. Only the IP Addresses
        /// are compared. Similar to the
        /// <see cref="GetHashCode()">GetHashCode()</see>
        /// method, the comparison
        /// is based on the integer IP address and not the string representation.
        /// </remarks>
        public override bool Equals(object obj)
        {
            return (obj != null)
                    && (obj is NbtAddress)
                    && (((NbtAddress)obj).Address == Address);
        }

        /// <summary>
        /// Returns the
        /// <see cref="string">string</see>
        /// representaion of this address.
        /// </summary>
        public override string ToString()
        {
            return HostName + "/" + GetHostAddress();
        }
    }
}