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
using SharpCifs.Dcerpc.Ndr;

namespace SharpCifs.Dcerpc.Msrpc
{
    internal class Netdfs
    {
        public static string GetSyntax()
        {
            return "4fc742e0-4a10-11cf-8273-00aa004ae673:3.0";
        }

        public const int DfsVolumeFlavorStandalone = unchecked(0x100);

        public const int DfsVolumeFlavorAdBlob = unchecked(0x200);

        public const int DfsStorageStateOffline = unchecked(0x0001);

        public const int DfsStorageStateOnline = unchecked(0x0002);

        public const int DfsStorageStateActive = unchecked(0x0004);

        internal class DfsInfo1 : NdrObject
        {
            public string EntryPath;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_referent(EntryPath, 1);
                if (EntryPath != null)
                {
                    dst = dst.Deferred;
                    dst.Enc_ndr_string(EntryPath);
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                int entryPathp = src.Dec_ndr_long();
                if (entryPathp != 0)
                {
                    src = src.Deferred;
                    EntryPath = src.Dec_ndr_string();
                }
            }
        }

        internal class DfsEnumArray1 : NdrObject
        {
            public int Count;

            public DfsInfo1[] S;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_long(Count);
                dst.Enc_ndr_referent(S, 1);
                if (S != null)
                {
                    dst = dst.Deferred;
                    int ss = Count;
                    dst.Enc_ndr_long(ss);
                    int si = dst.Index;
                    dst.Advance(4 * ss);
                    dst = dst.Derive(si);
                    for (int i = 0; i < ss; i++)
                    {
                        S[i].Encode(dst);
                    }
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                Count = src.Dec_ndr_long();
                int sp = src.Dec_ndr_long();
                if (sp != 0)
                {
                    src = src.Deferred;
                    int ss = src.Dec_ndr_long();
                    int si = src.Index;
                    src.Advance(4 * ss);
                    if (S == null)
                    {
                        if (ss < 0 || ss > unchecked(0xFFFF))
                        {
                            throw new NdrException(NdrException.InvalidConformance);
                        }
                        S = new DfsInfo1[ss];
                    }
                    src = src.Derive(si);
                    for (int i = 0; i < ss; i++)
                    {
                        if (S[i] == null)
                        {
                            S[i] = new DfsInfo1();
                        }
                        S[i].Decode(src);
                    }
                }
            }
        }
    }
}