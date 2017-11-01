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
using System.Text;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;
using System.Globalization;

namespace SharpCifs.Netbios
{
    internal sealed class Name
    {
        private const int TypeOffset = 31;

        private const int ScopeOffset = 33;

        private static readonly string DefaultScope
            = Config.GetProperty("jcifs.netbios.scope");

        public string name;

        public string Scope;

        public int HexCode;

        internal int SrcHashCode;

        public Name()
        {
        }

        public Name(string name, int hexCode, string scope)
        {
            if (name.Length > 15)
            {
                name = Runtime.Substring(name, 0, 15);
            }
            this.name = name.ToUpper(CultureInfo.InvariantCulture);
            this.HexCode = hexCode;
            this.Scope = !string.IsNullOrEmpty(scope) ? scope : DefaultScope;
            SrcHashCode = 0;
        }

        public override int GetHashCode()
        {
            int result;
            result = name.GetHashCode();
            result += 65599 * HexCode;
            result += 65599 * SrcHashCode;
            if (Scope != null && Scope.Length != 0)
            {
                result += Scope.GetHashCode();
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            Name n;
            if (!(obj is Name))
            {
                return false;
            }
            n = (Name)obj;
            if (Scope == null && n.Scope == null)
            {
                return name.Equals(n.name) && HexCode == n.HexCode;
            }
            return name.Equals(n.name) && HexCode == n.HexCode && Scope.Equals(n.Scope);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            //return "";

            string n = name;
            // fix MSBROWSE name
            if (n == null)
            {
                n = "null";
            }
            else
            {
                if (n[0] == unchecked(0x01))
                {
                    char[] c = n.ToCharArray();
                    c[0] = '.';
                    c[1] = '.';
                    c[14] = '.';
                    n = new string(c);
                }
            }
            sb.Append(n).Append("<").Append(Hexdump.ToHexString(HexCode, 2)).Append(">");
            if (Scope != null)
            {
                sb.Append(".").Append(Scope);
            }
            return sb.ToString();
        }
    }
}