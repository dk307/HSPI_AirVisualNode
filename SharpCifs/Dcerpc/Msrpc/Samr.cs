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
    internal class Samr
    {
        public static string GetSyntax()
        {
            return "12345778-1234-abcd-ef00-0123456789ac:1.0";
        }

        public const int AcbDisabled = 1;

        public const int AcbHomdirreq = 2;

        public const int AcbPwnotreq = 4;

        public const int AcbTempdup = 8;

        public const int AcbNormal = 16;

        public const int AcbMns = 32;

        public const int AcbDomtrust = 64;

        public const int AcbWstrust = 128;

        public const int AcbSvrtrust = 256;

        public const int AcbPwnoexp = 512;

        public const int AcbAutolock = 1024;

        public const int AcbEncTxtPwdAllowed = 2048;

        public const int AcbSmartcardRequired = 4096;

        public const int AcbTrustedForDelegation = 8192;

        public const int AcbNotDelegated = 16384;

        public const int AcbUseDesKeyOnly = 32768;

        public const int AcbDontRequirePreauth = 65536;

        internal class SamrSamEntry : NdrObject
        {
            public int Idx;

            public Rpc.Unicode_string Name;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_long(Idx);
                dst.Enc_ndr_short(Name.Length);
                dst.Enc_ndr_short(Name.MaximumLength);
                dst.Enc_ndr_referent(Name.Buffer, 1);
                if (Name.Buffer != null)
                {
                    dst = dst.Deferred;
                    int nameBufferl = Name.Length / 2;
                    int nameBuffers = Name.MaximumLength / 2;
                    dst.Enc_ndr_long(nameBuffers);
                    dst.Enc_ndr_long(0);
                    dst.Enc_ndr_long(nameBufferl);
                    int nameBufferi = dst.Index;
                    dst.Advance(2 * nameBufferl);
                    dst = dst.Derive(nameBufferi);
                    for (int i = 0; i < nameBufferl; i++)
                    {
                        dst.Enc_ndr_short(Name.Buffer[i]);
                    }
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                Idx = src.Dec_ndr_long();
                src.Align(4);
                if (Name == null)
                {
                    Name = new Rpc.Unicode_string();
                }
                Name.Length = (short)src.Dec_ndr_short();
                Name.MaximumLength = (short)src.Dec_ndr_short();
                int nameBufferp = src.Dec_ndr_long();
                if (nameBufferp != 0)
                {
                    src = src.Deferred;
                    int nameBuffers = src.Dec_ndr_long();
                    src.Dec_ndr_long();
                    int nameBufferl = src.Dec_ndr_long();
                    int nameBufferi = src.Index;
                    src.Advance(2 * nameBufferl);
                    if (Name.Buffer == null)
                    {
                        if (nameBuffers < 0 || nameBuffers > unchecked(0xFFFF))
                        {
                            throw new NdrException(NdrException.InvalidConformance);
                        }
                        Name.Buffer = new short[nameBuffers];
                    }
                    src = src.Derive(nameBufferi);
                    for (int i = 0; i < nameBufferl; i++)
                    {
                        Name.Buffer[i] = (short)src.Dec_ndr_short();
                    }
                }
            }
        }

        public const int SeGroupMandatory = 1;

        public const int SeGroupEnabledByDefault = 2;

        public const int SeGroupEnabled = 4;

        public const int SeGroupOwner = 8;

        public const int SeGroupUseForDenyOnly = 16;

        public const int SeGroupResource = 536870912;

        public const int SeGroupLogonId = -1073741824;
    }
}