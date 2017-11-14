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
using System.Collections.Generic;
using System.IO;
using System.Net;
using SharpCifs.Netbios;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
    internal sealed class SmbSession
    {
        private static readonly string LogonShare
            = Config.GetProperty("jcifs.smb.client.logonShare", null);

        private static readonly int LookupRespLimit
            = Config.GetInt("jcifs.netbios.lookupRespLimit", 3);

        private static readonly string Domain
            = Config.GetProperty("jcifs.smb.client.domain", null);

        private static readonly string Username
            = Config.GetProperty("jcifs.smb.client.username", null);

        private static readonly int CachePolicy
            = Config.GetInt("jcifs.netbios.cachePolicy", 60 * 10) * 60;

        internal int ConnectionState;

        internal int Uid;

        internal List<object> Trees;

        private UniAddress _address;

        private int _localPort;

        private IPAddress _localAddr;

        internal SmbTransport transport;

        internal NtlmPasswordAuthentication Auth;

        internal long Expiration;

        internal string NetbiosName;

        internal SmbSession(UniAddress address,
                             IPAddress localAddr,
                            int localPort,
                            NtlmPasswordAuthentication auth)
        {
            // Transport parameters allows trans to be removed from CONNECTIONS
            this._address = address;
            this._localAddr = localAddr;
            this._localPort = localPort;
            this.Auth = auth;
            Trees = new List<object>();
            ConnectionState = 0;
        }

        internal SmbTree GetSmbTree(string share, string service)
        {
            lock (this)
            {
                SmbTree t;
                if (share == null)
                {
                    share = "IPC$";
                }
                /*
                for (IEnumeration e = trees.GetEnumerator(); e.MoveNext(); )
				{
					t = (SmbTree)e.Current;
					if (t.Matches(share, service))
					{
						return t;
					}
				}
                */
                foreach (var e in Trees)
                {
                    t = (SmbTree)e;
                    if (t.Matches(share, service))
                    {
                        return t;
                    }
                }

                t = new SmbTree(this, share, service);
                Trees.Add(t);
                return t;
            }
        }

        internal bool Matches(NtlmPasswordAuthentication auth)
        {
            return this.Auth == auth || this.Auth.Equals(auth);
        }

        internal SmbTransport Transport()
        {
            lock (this)
            {
                if (transport == null)
                {
                    transport = SmbTransport.GetSmbTransport(_address,
                                                              _localAddr,
                                                             _localPort,
                                                             null);
                }
                return transport;
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void Send(ServerMessageBlock request, ServerMessageBlock response)
        {
            lock (Transport())
            {
                if (response != null)
                {
                    response.Received = false;
                }
                Expiration = Runtime.CurrentTimeMillis() + SmbConstants.SoTimeout;
                SessionSetup(request, response);
                if (response != null && response.Received)
                {
                    return;
                }
                if (request is SmbComTreeConnectAndX)
                {
                    SmbComTreeConnectAndX tcax = (SmbComTreeConnectAndX)request;
                    if (NetbiosName != null && tcax.path.EndsWith("\\IPC$"))
                    {
                        tcax.path = "\\\\" + NetbiosName + "\\IPC$";
                    }
                }
                request.Uid = Uid;
                request.Auth = Auth;
                try
                {
                    transport.Send(request, response);
                }
                catch (SmbException)
                {
                    if (request is SmbComTreeConnectAndX)
                    {
                        Logoff(true);
                    }
                    request.Digest = null;
                    throw;
                }
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void SessionSetup(ServerMessageBlock andx, ServerMessageBlock andxResponse)
        {
            lock (Transport())
            {
                NtlmContext nctx = null;
                SmbException ex = null;
                SmbComSessionSetupAndX request;
                SmbComSessionSetupAndXResponse response;
                byte[] token = new byte[0];
                int state = 10;
                while (ConnectionState != 0)
                {
                    if (ConnectionState == 2 || ConnectionState == 3)
                    {
                        // connected or disconnecting
                        return;
                    }
                    try
                    {
                        Runtime.Wait(transport);
                    }
                    catch (Exception ie)
                    {
                        throw new SmbException(ie.Message, ie);
                    }
                }
                ConnectionState = 1;
                // trying ...
                try
                {
                    transport.Connect();
                    if (transport.Log.Level >= 4)
                    {
                        transport.Log.WriteLine("sessionSetup: accountName=" + Auth.Username
                                                + ",primaryDomain=" + Auth.Domain);
                    }
                    Uid = 0;
                    do
                    {
                        switch (state)
                        {
                            case 10:
                                {
                                    if (Auth != NtlmPasswordAuthentication.Anonymous
                                        && transport.HasCapability(SmbConstants.CapExtendedSecurity))
                                    {
                                        state = 20;
                                        break;
                                    }
                                    request = new SmbComSessionSetupAndX(this, andx, Auth);
                                    response = new SmbComSessionSetupAndXResponse(andxResponse);
                                    if (transport.IsSignatureSetupRequired(Auth))
                                    {
                                        if (Auth.HashesExternal
                                            && NtlmPasswordAuthentication.DefaultPassword
                                                != NtlmPasswordAuthentication.Blank)
                                        {
                                            transport.GetSmbSession(NtlmPasswordAuthentication.Default)
                                                     .GetSmbTree(LogonShare, null)
                                                     .TreeConnect(null, null);
                                        }
                                        else
                                        {
                                            byte[] signingKey
                                                = Auth.GetSigningKey(transport.Server.EncryptionKey);
                                            request.Digest = new SigningDigest(signingKey, false);
                                        }
                                    }
                                    request.Auth = Auth;
                                    try
                                    {
                                        transport.Send(request, response);
                                    }
                                    catch (SmbAuthException)
                                    {
                                        throw;
                                    }
                                    catch (SmbException se)
                                    {
                                        ex = se;
                                    }
                                    if (response.IsLoggedInAsGuest
                                        && Runtime.EqualsIgnoreCase("GUEST", Auth.Username) == false
                                        && transport.Server.Security != SmbConstants.SecurityShare
                                        && Auth != NtlmPasswordAuthentication.Anonymous)
                                    {
                                        throw new SmbAuthException(NtStatus.NtStatusLogonFailure);
                                    }
                                    if (ex != null)
                                    {
                                        throw ex;
                                    }
                                    Uid = response.Uid;
                                    if (request.Digest != null)
                                    {
                                        transport.Digest = request.Digest;
                                    }
                                    ConnectionState = 2;
                                    state = 0;
                                    break;
                                }

                            case 20:
                                {
                                    if (nctx == null)
                                    {
                                        bool doSigning
                                            = (transport.Flags2
                                               & SmbConstants.Flags2SecuritySignatures) != 0;
                                        nctx = new NtlmContext(Auth, doSigning);
                                    }
                                    if (SmbTransport.LogStatic.Level >= 4)
                                    {
                                        SmbTransport.LogStatic.WriteLine(nctx);
                                    }
                                    if (nctx.IsEstablished())
                                    {
                                        NetbiosName = nctx.GetNetbiosName();
                                        ConnectionState = 2;
                                        state = 0;
                                        break;
                                    }
                                    try
                                    {
                                        token = nctx.InitSecContext(token, 0, token.Length);
                                    }
                                    catch (SmbException)
                                    {
                                        try
                                        {
                                            transport.Disconnect(true);
                                        }
                                        catch (IOException)
                                        {
                                        }
                                        Uid = 0;
                                        throw;
                                    }
                                    if (token != null)
                                    {
                                        request = new SmbComSessionSetupAndX(this, null, token);
                                        response = new SmbComSessionSetupAndXResponse(null);
                                        if (transport.IsSignatureSetupRequired(Auth))
                                        {
                                            byte[] signingKey = nctx.GetSigningKey();
                                            if (signingKey != null)
                                            {
                                                request.Digest = new SigningDigest(signingKey, true);
                                            }
                                        }
                                        request.Uid = Uid;
                                        Uid = 0;
                                        try
                                        {
                                            transport.Send(request, response);
                                        }
                                        catch (SmbAuthException)
                                        {
                                            throw;
                                        }
                                        catch (SmbException se)
                                        {
                                            ex = se;
                                            try
                                            {
                                                transport.Disconnect(true);
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                        if (response.IsLoggedInAsGuest
                                            && Runtime.EqualsIgnoreCase("GUEST", Auth.Username)
                                                    == false)
                                        {
                                            throw new SmbAuthException(NtStatus.NtStatusLogonFailure);
                                        }
                                        if (ex != null)
                                        {
                                            throw ex;
                                        }
                                        Uid = response.Uid;
                                        if (request.Digest != null)
                                        {
                                            transport.Digest = request.Digest;
                                        }
                                        token = response.Blob;
                                    }
                                    break;
                                }

                            default:
                                {
                                    throw new SmbException("Unexpected session setup state: " + state);
                                }
                        }
                    }
                    while (state != 0);
                }
                catch (SmbException)
                {
                    Logoff(true);
                    ConnectionState = 0;
                    throw;
                }
                finally
                {
                    Runtime.NotifyAll(transport);
                }
            }
        }

        internal void Logoff(bool inError)
        {
            lock (Transport())
            {
                if (ConnectionState != 2)
                {
                    // not-connected
                    return;
                }
                ConnectionState = 3;
                // disconnecting
                NetbiosName = null;

                foreach (SmbTree t in Trees)
                {
                    t.TreeDisconnect(inError);
                }

                if (!inError && transport.Server.Security != SmbConstants.SecurityShare)
                {
                    SmbComLogoffAndX request = new SmbComLogoffAndX(null);
                    request.Uid = Uid;
                    try
                    {
                        transport.Send(request, null);
                    }
                    catch (SmbException)
                    {
                    }
                    Uid = 0;
                }
                ConnectionState = 0;
                Runtime.NotifyAll(transport);
            }
        }

        public override string ToString()
        {
            return "SmbSession[accountName=" + Auth.Username
                            + ",primaryDomain=" + Auth.Domain
                            + ",uid=" + Uid
                            + ",connectionState=" + ConnectionState + "]";
        }
    }
}