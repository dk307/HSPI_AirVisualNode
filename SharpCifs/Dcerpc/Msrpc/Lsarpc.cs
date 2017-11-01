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
    internal class Lsarpc
    {
        public static string GetSyntax()
        {
            return "12345778-1234-abcd-ef00-0123456789ab:0.0";
        }

        internal class LsarQosInfo : NdrObject
        {
            public int Length;

            public short ImpersonationLevel;

            public byte ContextMode;

            public byte EffectiveOnly;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_long(Length);
                dst.Enc_ndr_short(ImpersonationLevel);
                dst.Enc_ndr_small(ContextMode);
                dst.Enc_ndr_small(EffectiveOnly);
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                Length = src.Dec_ndr_long();
                ImpersonationLevel = (short)src.Dec_ndr_short();
                ContextMode = unchecked((byte)src.Dec_ndr_small());
                EffectiveOnly = unchecked((byte)src.Dec_ndr_small());
            }
        }

        internal class LsarObjectAttributes : NdrObject
        {
            public int Length;

            public NdrSmall RootDirectory;

            public Rpc.Unicode_string ObjectName;

            public int Attributes;

            public int SecurityDescriptor;

            public LsarQosInfo SecurityQualityOfService;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_long(Length);
                dst.Enc_ndr_referent(RootDirectory, 1);
                dst.Enc_ndr_referent(ObjectName, 1);
                dst.Enc_ndr_long(Attributes);
                dst.Enc_ndr_long(SecurityDescriptor);
                dst.Enc_ndr_referent(SecurityQualityOfService, 1);
                if (RootDirectory != null)
                {
                    dst = dst.Deferred;
                    RootDirectory.Encode(dst);
                }
                if (ObjectName != null)
                {
                    dst = dst.Deferred;
                    ObjectName.Encode(dst);
                }
                if (SecurityQualityOfService != null)
                {
                    dst = dst.Deferred;
                    SecurityQualityOfService.Encode(dst);
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                Length = src.Dec_ndr_long();
                int rootDirectoryp = src.Dec_ndr_long();
                int objectNamep = src.Dec_ndr_long();
                Attributes = src.Dec_ndr_long();
                SecurityDescriptor = src.Dec_ndr_long();
                int securityQualityOfServicep = src.Dec_ndr_long();
                if (rootDirectoryp != 0)
                {
                    src = src.Deferred;
                    RootDirectory.Decode(src);
                }
                if (objectNamep != 0)
                {
                    if (ObjectName == null)
                    {
                        ObjectName = new Rpc.Unicode_string();
                    }
                    src = src.Deferred;
                    ObjectName.Decode(src);
                }
                if (securityQualityOfServicep != 0)
                {
                    if (SecurityQualityOfService == null)
                    {
                        SecurityQualityOfService = new LsarQosInfo();
                    }
                    src = src.Deferred;
                    SecurityQualityOfService.Decode(src);
                }
            }
        }

        internal class LsarDomainInfo : NdrObject
        {
            public Rpc.Unicode_string Name;

            public Rpc.SidT Sid;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_short(Name.Length);
                dst.Enc_ndr_short(Name.MaximumLength);
                dst.Enc_ndr_referent(Name.Buffer, 1);
                dst.Enc_ndr_referent(Sid, 1);
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
                if (Sid != null)
                {
                    dst = dst.Deferred;
                    Sid.Encode(dst);
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                src.Align(4);
                if (Name == null)
                {
                    Name = new Rpc.Unicode_string();
                }
                Name.Length = (short)src.Dec_ndr_short();
                Name.MaximumLength = (short)src.Dec_ndr_short();
                int nameBufferp = src.Dec_ndr_long();
                int sidp = src.Dec_ndr_long();
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
                if (sidp != 0)
                {
                    if (Sid == null)
                    {
                        Sid = new Rpc.SidT();
                    }
                    src = src.Deferred;
                    Sid.Decode(src);
                }
            }
        }

        public const int PolicyInfoAuditEvents = 2;

        public const int PolicyInfoPrimaryDomain = 3;

        public const int PolicyInfoAccountDomain = 5;

        public const int PolicyInfoServerRole = 6;

        public const int PolicyInfoModification = 9;

        public const int PolicyInfoDnsDomain = 12;

        internal class LsarSidPtr : NdrObject
        {
            public Rpc.SidT Sid;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_referent(Sid, 1);
                if (Sid != null)
                {
                    dst = dst.Deferred;
                    Sid.Encode(dst);
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                int sidp = src.Dec_ndr_long();
                if (sidp != 0)
                {
                    if (Sid == null)
                    {
                        Sid = new Rpc.SidT();
                    }
                    src = src.Deferred;
                    Sid.Decode(src);
                }
            }
        }

        public const int SidNameUseNone = 0;

        public const int SidNameUser = 1;

        public const int SidNameDomGrp = 2;

        public const int SidNameDomain = 3;

        public const int SidNameAlias = 4;

        public const int SidNameWknGrp = 5;

        public const int SidNameDeleted = 6;

        public const int SidNameInvalid = 7;

        public const int SidNameUnknown = 8;

        internal class LsarTrustInformation : NdrObject
        {
            public Rpc.Unicode_string Name;

            public Rpc.SidT Sid;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_short(Name.Length);
                dst.Enc_ndr_short(Name.MaximumLength);
                dst.Enc_ndr_referent(Name.Buffer, 1);
                dst.Enc_ndr_referent(Sid, 1);
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
                if (Sid != null)
                {
                    dst = dst.Deferred;
                    Sid.Encode(dst);
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                src.Align(4);
                if (Name == null)
                {
                    Name = new Rpc.Unicode_string();
                }
                Name.Length = (short)src.Dec_ndr_short();
                Name.MaximumLength = (short)src.Dec_ndr_short();
                int nameBufferp = src.Dec_ndr_long();
                int sidp = src.Dec_ndr_long();
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
                if (sidp != 0)
                {
                    if (Sid == null)
                    {
                        Sid = new Rpc.SidT();
                    }
                    src = src.Deferred;
                    Sid.Decode(src);
                }
            }
        }

        internal class LsarRefDomainList : NdrObject
        {
            public int Count;

            public LsarTrustInformation[] Domains;

            public int MaxCount;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_long(Count);
                dst.Enc_ndr_referent(Domains, 1);
                dst.Enc_ndr_long(MaxCount);
                if (Domains != null)
                {
                    dst = dst.Deferred;
                    int domainss = Count;
                    dst.Enc_ndr_long(domainss);
                    int domainsi = dst.Index;
                    dst.Advance(12 * domainss);
                    dst = dst.Derive(domainsi);
                    for (int i = 0; i < domainss; i++)
                    {
                        Domains[i].Encode(dst);
                    }
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                Count = src.Dec_ndr_long();
                int domainsp = src.Dec_ndr_long();
                MaxCount = src.Dec_ndr_long();
                if (domainsp != 0)
                {
                    src = src.Deferred;
                    int domainss = src.Dec_ndr_long();
                    int domainsi = src.Index;
                    src.Advance(12 * domainss);
                    if (Domains == null)
                    {
                        if (domainss < 0 || domainss > unchecked(0xFFFF))
                        {
                            throw new NdrException(NdrException.InvalidConformance);
                        }
                        Domains = new LsarTrustInformation[domainss];
                    }
                    src = src.Derive(domainsi);
                    for (int i = 0; i < domainss; i++)
                    {
                        if (Domains[i] == null)
                        {
                            Domains[i] = new LsarTrustInformation();
                        }
                        Domains[i].Decode(src);
                    }
                }
            }
        }

        internal class LsarTranslatedName : NdrObject
        {
            public short SidType;

            public Rpc.Unicode_string Name;

            public int SidIndex;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_short(SidType);
                dst.Enc_ndr_short(Name.Length);
                dst.Enc_ndr_short(Name.MaximumLength);
                dst.Enc_ndr_referent(Name.Buffer, 1);
                dst.Enc_ndr_long(SidIndex);
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
                SidType = (short)src.Dec_ndr_short();
                src.Align(4);
                if (Name == null)
                {
                    Name = new Rpc.Unicode_string();
                }
                Name.Length = (short)src.Dec_ndr_short();
                Name.MaximumLength = (short)src.Dec_ndr_short();
                int nameBufferp = src.Dec_ndr_long();
                SidIndex = src.Dec_ndr_long();
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

        internal class LsarTransNameArray : NdrObject
        {
            public int Count;

            public LsarTranslatedName[] Names;

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Encode(NdrBuffer dst)
            {
                dst.Align(4);
                dst.Enc_ndr_long(Count);
                dst.Enc_ndr_referent(Names, 1);
                if (Names != null)
                {
                    dst = dst.Deferred;
                    int namess = Count;
                    dst.Enc_ndr_long(namess);
                    int namesi = dst.Index;
                    dst.Advance(16 * namess);
                    dst = dst.Derive(namesi);
                    for (int i = 0; i < namess; i++)
                    {
                        Names[i].Encode(dst);
                    }
                }
            }

            /// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
            public override void Decode(NdrBuffer src)
            {
                src.Align(4);
                Count = src.Dec_ndr_long();
                int namesp = src.Dec_ndr_long();
                if (namesp != 0)
                {
                    src = src.Deferred;
                    int namess = src.Dec_ndr_long();
                    int namesi = src.Index;
                    src.Advance(16 * namess);
                    if (Names == null)
                    {
                        if (namess < 0 || namess > unchecked(0xFFFF))
                        {
                            throw new NdrException(NdrException.InvalidConformance);
                        }
                        Names = new LsarTranslatedName[namess];
                    }
                    src = src.Derive(namesi);
                    for (int i = 0; i < namess; i++)
                    {
                        if (Names[i] == null)
                        {
                            Names[i] = new LsarTranslatedName();
                        }
                        Names[i].Decode(src);
                    }
                }
            }
        }
    }
}