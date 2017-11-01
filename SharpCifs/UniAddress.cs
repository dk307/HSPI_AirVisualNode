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
using System.IO;
using System.Linq;
using System.Net;
using SharpCifs.Netbios;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;
using Extensions = SharpCifs.Util.Sharpen.Extensions;
using System.Threading.Tasks;

namespace SharpCifs
{
    /// <summary>
    /// <p>Under normal conditions it is not necessary to use
    /// this class to use jCIFS properly.
    /// </summary>
    /// <remarks>
    /// <p>Under normal conditions it is not necessary to use
    /// this class to use jCIFS properly. Name resolusion is
    /// handled internally to the <code>jcifs.smb</code> package.
    /// <p>
    /// This class is a wrapper for both
    /// <see cref="Jcifs.Netbios.NbtAddress">Jcifs.Netbios.NbtAddress</see>
    /// and
    /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
    /// . The name resolution mechanisms
    /// used will systematically query all available configured resolution
    /// services including WINS, broadcasts, DNS, and LMHOSTS. See
    /// <a href="../../resolver.html">Setting Name Resolution Properties</a>
    /// and the <code>jcifs.resolveOrder</code> property. Changing
    /// jCIFS name resolution properties can greatly affect the behavior of
    /// the client and may be necessary for proper operation.
    /// <p>
    /// This class should be used in favor of <tt>InetAddress</tt> to resolve
    /// hostnames on LANs and WANs that support a mixture of NetBIOS/WINS and
    /// DNS resolvable hosts.
    /// </remarks>
    internal class UniAddress
    {
        internal static bool IsDotQuadIp(string hostname)
        {
            if (char.IsDigit(hostname[0]))
            {
                int i;
                int len;
                int dots;
                char[] data;
                i = dots = 0;
                len = hostname.Length;
                data = hostname.ToCharArray();
                while (i < len && char.IsDigit(data[i++]))
                {
                    if (i == len && dots == 3)
                    {
                        // probably an IP address
                        return true;
                    }
                    if (i < len && data[i] == '.')
                    {
                        dots++;
                        i++;
                    }
                }
            }
            return false;
        }

        /// <exception cref="UnknownHostException"></exception>
        public static UniAddress GetAllByName(string hostname, bool possibleNtDomainOrWorkgroup)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                throw new UnknownHostException();
            }
            if (IsDotQuadIp(hostname))
            {
                return new UniAddress(NbtAddress.GetByName(hostname));
            }

            // Success
            // Failure
            throw new UnknownHostException(hostname);
        }

        internal object Addr;

        /// <summary>
        /// Create a <tt>UniAddress</tt> by wrapping an <tt>InetAddress</tt> or
        /// <tt>NbtAddress</tt>.
        /// </summary>
        /// <remarks>
        /// Create a <tt>UniAddress</tt> by wrapping an <tt>InetAddress</tt> or
        /// <tt>NbtAddress</tt>.
        /// </remarks>
        public UniAddress(object addr)
        {
            if (addr == null)
            {
                throw new ArgumentException();
            }
            this.Addr = addr;
        }

        /// <summary>Return the IP address of this address as a 32 bit integer.</summary>
        /// <remarks>Return the IP address of this address as a 32 bit integer.</remarks>
        public override int GetHashCode()
        {
            return Addr.GetHashCode();
        }

        /// <summary>Compare two addresses for equality.</summary>
        /// <remarks>
        /// Compare two addresses for equality. Two <tt>UniAddress</tt>s are equal
        /// if they are both <tt>UniAddress</tt>' and refer to the same IP address.
        /// </remarks>
        public override bool Equals(object obj)
        {
            return obj is UniAddress && Addr.Equals(((UniAddress)obj).Addr);
        }

        /// <summary>Return the underlying <tt>NbtAddress</tt> or <tt>InetAddress</tt>.</summary>
        /// <remarks>Return the underlying <tt>NbtAddress</tt> or <tt>InetAddress</tt>.</remarks>
        public virtual object GetAddress()
        {
            return Addr;
        }

        /// <summary>Return the hostname of this address such as "MYCOMPUTER".</summary>
        /// <remarks>Return the hostname of this address such as "MYCOMPUTER".</remarks>
        public virtual string GetHostName()
        {
            if (Addr is NbtAddress)
            {
                return ((NbtAddress)Addr).GetHostName();
            }
            return ((IPAddress)Addr).GetHostAddress();
        }

        /// <summary>Return the IP address as text such as "192.168.1.15".</summary>
        /// <remarks>Return the IP address as text such as "192.168.1.15".</remarks>
        public virtual string GetHostAddress()
        {
            if (Addr is NbtAddress)
            {
                return ((NbtAddress)Addr).GetHostAddress();
            }
            return ((IPAddress)Addr).GetHostAddress();
        }

        public virtual IPAddress GetHostIpAddress()
        {
            return (IPAddress)Addr;
        }

        /// <summary>
        /// Return the a text representation of this address such as
        /// <tt>MYCOMPUTER/192.168.1.15</tt>.
        /// </summary>
        /// <remarks>
        /// Return the a text representation of this address such as
        /// <tt>MYCOMPUTER/192.168.1.15</tt>.
        /// </remarks>
        public override string ToString()
        {
            return Addr.ToString();
        }
    }
}