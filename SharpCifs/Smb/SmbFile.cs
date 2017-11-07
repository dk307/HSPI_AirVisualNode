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
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpCifs.Smb
{
    internal sealed class SmbFile : UrlConnection
    {
        internal const int ORdonly = 0x01;

        internal const int OWronly = 0x02;

        internal const int ORdwr = 0x03;

        internal const int OAppend = 0x04;

        internal const int OCreat = 0x0010;

        internal const int OExcl = 0x0020;

        internal const int OTrunc = 0x0040;

        /// <summary>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients (including other threads making calls into jCIFS)
        /// will not be permitted to access the target file and will receive "The
        /// file is being accessed by another process" message.
        /// </summary>
        /// <remarks>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients (including other threads making calls into jCIFS)
        /// will not be permitted to access the target file and will receive "The
        /// file is being accessed by another process" message.
        /// </remarks>
        public const int FileNoShare = 0x00;

        /// <summary>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to read from the target file while
        /// this file is open.
        /// </summary>
        /// <remarks>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to read from the target file while
        /// this file is open. This constant may be logically OR'd with other share
        /// access flags.
        /// </remarks>
        public const int FileShareRead = 0x01;

        /// <summary>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to write to the target file while
        /// this file is open.
        /// </summary>
        /// <remarks>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to write to the target file while
        /// this file is open. This constant may be logically OR'd with other share
        /// access flags.
        /// </remarks>
        public const int FileShareWrite = 0x02;

        /// <summary>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to delete the target file while
        /// this file is open.
        /// </summary>
        /// <remarks>
        /// When specified as the <tt>shareAccess</tt> constructor parameter,
        /// other SMB clients will be permitted to delete the target file while
        /// this file is open. This constant may be logically OR'd with other share
        /// access flags.
        /// </remarks>
        public const int FileShareDelete = 0x04;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> or set
        /// with <tt>setAttributes()</tt> will be read-only
        /// </summary>
        public const int AttrReadonly = 0x01;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> or set
        /// with <tt>setAttributes()</tt> will be hidden
        /// </summary>
        public const int AttrHidden = 0x02;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> or set
        /// with <tt>setAttributes()</tt> will be a system file
        /// </summary>
        public const int AttrSystem = 0x04;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> is
        /// a volume
        /// </summary>
        public const int AttrVolume = 0x08;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> is
        /// a directory
        /// </summary>
        public const int AttrDirectory = 0x10;

        /// <summary>
        /// A file with this bit on as returned by <tt>getAttributes()</tt> or set
        /// with <tt>setAttributes()</tt> is an archived file
        /// </summary>
        public const int AttrArchive = 0x20;

        internal const int AttrCompressed = 0x800;

        internal const int AttrNormal = 0x080;

        internal const int AttrTemporary = 0x100;

        internal const int AttrGetMask = 0x7FFF;

        internal const int AttrSetMask = 0x30A7;

        internal const int DefaultAttrExpirationPeriod = 5000;

        //internal static LogStream log = LogStream.GetInstance();
        public LogStream Log
        {
            get { return LogStream.GetInstance(); }
        }

        internal static long AttrExpirationPeriod;

        static SmbFile()
        {
            AttrExpirationPeriod
                = Config.GetLong("jcifs.smb.client.attrExpirationPeriod", DefaultAttrExpirationPeriod);
        }

        /// <summary>
        /// Returned by
        /// <see cref="GetType()">GetType()</see>
        /// if the resource this <tt>SmbFile</tt>
        /// represents is a regular file or directory.
        /// </summary>
        public const int TypeFilesystem = 0x01;

        private string _canon;

        private string _share;

        private long _size;

        private long _sizeExpiration;

        private int _shareAccess = FileShareRead | FileShareWrite | FileShareDelete;

        private SmbComBlankResponse _blankResp;

        internal NtlmPasswordAuthentication Auth;

        internal SmbTree Tree;

        internal string Unc;

        internal int Fid;

        internal int Type;

        internal bool Opened;

        internal int TreeNum;

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such as
        /// a file or directory.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such as
        /// a file or directory. See the description and examples of smb URLs above.
        /// </remarks>
        /// <param name="url">A URL string</param>
        /// <exception cref="System.UriFormatException">
        /// If the <code>parent</code> and <code>child</code> parameters
        /// do not follow the prescribed syntax
        /// </exception>
        public SmbFile(string url)
            : this(new Uri(url))
        {
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory from a <tt>URL</tt> object and an
        /// <tt>NtlmPasswordAuthentication</tt> object.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory from a <tt>URL</tt> object and an
        /// <tt>NtlmPasswordAuthentication</tt> object.
        /// </remarks>
        /// <param name="url">The URL of the target resource</param>
        /// <param name="auth">The credentials the client should use for authentication</param>
        public SmbFile(Uri url, NtlmPasswordAuthentication auth) :
            base(url)
        {
            this.Auth = auth ?? new NtlmPasswordAuthentication(url.GetUserInfo());
            GetUncPath0();
        }

        /// <summary>Constructs an SmbFile representing a file on an SMB network.</summary>
        /// <remarks>
        /// Constructs an SmbFile representing a file on an SMB network. The
        /// <tt>shareAccess</tt> parameter controls what permissions other
        /// clients have when trying to access the same file while this instance
        /// is still open. This value is either <tt>FILE_NO_SHARE</tt> or any
        /// combination of <tt>FILE_SHARE_READ</tt>, <tt>FILE_SHARE_WRITE</tt>,
        /// and <tt>FILE_SHARE_DELETE</tt> logically OR'd together.
        /// </remarks>
        /// <param name="url">A URL string</param>
        /// <param name="auth">The credentials the client should use for authentication</param>
        /// <param name="shareAccess">Specifies what access other clients have while this file is open.
        /// </param>
        /// <exception cref="System.UriFormatException">If the <code>url</code> parameter does not follow the prescribed syntax
        /// </exception>
        public SmbFile(string url, NtlmPasswordAuthentication auth, int shareAccess)
            : this(new Uri(url), auth)
        {
            // Initially null; set by getUncPath; dir must end with '/'
            // Can be null
            // For getDfsPath() and getServerWithDfs()
            // Cannot be null
            // Initially null
            // Initially null; set by getUncPath; never ends with '/'
            // Initially 0; set by open()
            if ((shareAccess & ~(FileShareRead | FileShareWrite | FileShareDelete)) != 0)
            {
                throw new RuntimeException("Illegal shareAccess parameter");
            }
            this._shareAccess = shareAccess;
        }

        /// <summary>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory from a <tt>URL</tt> object.
        /// </summary>
        /// <remarks>
        /// Constructs an SmbFile representing a resource on an SMB network such
        /// as a file or directory from a <tt>URL</tt> object.
        /// </remarks>
        /// <param name="url">The URL of the target resource</param>
        private SmbFile(Uri url)
            : this(url, new NtlmPasswordAuthentication(url.GetUserInfo()))
        {
        }

        private SmbComBlankResponse Blank_resp()
        {
            if (_blankResp == null)
            {
                _blankResp = new SmbComBlankResponse();
            }
            return _blankResp;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void Send(ServerMessageBlock request, ServerMessageBlock response)
        {
            Tree.Send(request, response);
        }

        internal UniAddress Addresses;

        /// <exception cref="UnknownHostException"></exception>
        internal UniAddress GetAddress()
        {
            return Addresses;
        }

        /// <exception cref="UnknownHostException"></exception>
        internal UniAddress GetFirstAddress()
        {
            string host = Url.GetHost();
            string path = Url.AbsolutePath;

            Addresses = UniAddress.GetAllByName(host, false);
            return Addresses;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void Connect0()
        {
            try
            {
                Connect();
            }
            catch (UnknownHostException uhe)
            {
                throw new SmbException("Failed to connect to server", uhe);
            }
            catch (IOException ioe)
            {
                throw new SmbException("Failed to connect to server", ioe);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal void DoConnect()
        {
            SmbTransport trans;
            UniAddress addr;
            addr = GetAddress();

            if (Tree != null && Tree.Session.transport.Address.Equals(addr))
            {
                trans = Tree.Session.transport;
            }
            else
            {
                trans = SmbTransport.GetSmbTransport(addr, Url.Port);
                Tree = trans.GetSmbSession(Auth).GetSmbTree(_share, null);
            }

            try
            {
                if (Log.Level >= 3)
                {
                    Log.WriteLine("doConnect: " + addr);
                }
                Tree.TreeConnect(null, null);
            }
            catch (SmbAuthException sae)
            {
                NtlmPasswordAuthentication a;
                SmbSession ssn;
                if (_share == null)
                {
                    // IPC$ - try "anonymous" credentials
                    ssn = trans.GetSmbSession(NtlmPasswordAuthentication.Null);
                    Tree = ssn.GetSmbTree(null, null);
                    Tree.TreeConnect(null, null);
                }
                else
                {
                    if ((a = NtlmAuthenticator.RequestNtlmPasswordAuthentication(Url.ToString(), sae))
                        != null)
                    {
                        Auth = a;
                        ssn = trans.GetSmbSession(Auth);

                        Tree.TreeConnect(null, null);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>It is not necessary to call this method directly.</summary>
        /// <remarks>
        /// It is not necessary to call this method directly. This is the
        /// <tt>URLConnection</tt> implementation of <tt>connect()</tt>.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public void Connect()
        {
            if (IsConnected())
            {
                return;
            }
            GetUncPath0();
            GetFirstAddress();
            for (;;)
            {
                try
                {
                    DoConnect();
                    return;
                }
                catch (SmbAuthException)
                {
                    throw;
                }
            }
        }

        internal bool IsConnected()
        {
            return Tree != null && Tree.ConnectionState == 2;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal int Open0(int flags, int access, int attrs, int options)
        {
            int f;
            Connect0();
            if (Log.Level >= 3)
            {
                Log.WriteLine("open0: " + Unc);
            }
            if (Tree.Session.transport.HasCapability(SmbConstants.CapNtSmbs))
            {
                SmbComNtCreateAndXResponse response = new SmbComNtCreateAndXResponse();
                SmbComNtCreateAndX request = new SmbComNtCreateAndX(Unc,
                                                                    flags,
                                                                    access,
                                                                    _shareAccess,
                                                                    attrs,
                                                                    options,
                                                                    null);

                Send(request, response);
                f = response.Fid;
            }
            else
            {
                SmbComOpenAndXResponse response = new SmbComOpenAndXResponse();
                Send(new SmbComOpenAndX(Unc, access, flags, null), response);
                f = response.Fid;
            }
            return f;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void Open(int flags, int access, int attrs, int options)
        {
            if (IsOpen())
            {
                return;
            }
            Fid = Open0(flags, access, attrs, options);
            Opened = true;
            TreeNum = Tree.TreeNum;
        }

        internal bool IsOpen()
        {
            bool ans = Opened && IsConnected() && TreeNum == Tree.TreeNum;
            return ans;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void Close(int f, long lastWriteTime)
        {
            if (Log.Level >= 3)
            {
                Log.WriteLine("close: " + f);
            }
            Send(new SmbComClose(f, lastWriteTime), Blank_resp());
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void Close(long lastWriteTime)
        {
            if (IsOpen() == false)
            {
                return;
            }
            Close(Fid, lastWriteTime);
            Opened = false;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal void Close()
        {
            Close(0L);
        }

        /// <summary>
        /// Everything but the last component of the URL representing this SMB
        /// resource is effectivly it's parent.
        /// </summary>
        /// <remarks>
        /// Everything but the last component of the URL representing this SMB
        /// resource is effectivly it's parent. The root URL <code>smb://</code>
        /// does not have a parent. In this case <code>smb://</code> is returned.
        /// </remarks>
        /// <returns>
        /// The parent directory of this SMB resource or
        /// <code>smb://</code> if the resource refers to the root of the URL
        /// hierarchy which incedentally is also <code>smb://</code>.
        /// </returns>
        public string GetParent()
        {
            string str = Url.Authority;
            if (str.Length > 0)
            {
                StringBuilder sb = new StringBuilder("smb://");
                sb.Append(str);
                GetUncPath0();
                if (_canon.Length > 1)
                {
                    sb.Append(_canon);
                }
                else
                {
                    sb.Append('/');
                }
                str = sb.ToString();
                int i = str.Length - 2;
                while (str[i] != '/')
                {
                    i--;
                }
                return Runtime.Substring(str, 0, i + 1);
            }
            return "smb://";
        }

        internal string GetUncPath0()
        {
            if (Unc == null)
            {
                char[] instr = Url.LocalPath.ToCharArray();
                char[] outstr = new char[instr.Length];
                int length = instr.Length;
                int i;
                int o;
                int state;

                state = 0;
                o = 0;
                for (i = 0; i < length; i++)
                {
                    switch (state)
                    {
                        case 0:
                            {
                                if (instr[i] != '/')
                                {
                                    return null;
                                }
                                outstr[o++] = instr[i];
                                state = 1;
                                break;
                            }

                        case 1:
                            {
                                if (instr[i] == '/')
                                {
                                    break;
                                }
                                if (instr[i] == '.' && ((i + 1) >= length || instr[i + 1] == '/'))
                                {
                                    i++;
                                    break;
                                }
                                if ((i + 1) < length
                                    && instr[i] == '.'
                                    && instr[i + 1] == '.'
                                    && ((i + 2) >= length || instr[i + 2] == '/'))
                                {
                                    i += 2;
                                    if (o == 1)
                                    {
                                        break;
                                    }
                                    do
                                    {
                                        o--;
                                    }
                                    while (o > 1 && outstr[o - 1] != '/');
                                    break;
                                }
                                state = 2;
                                goto case 2;
                            }

                        case 2:
                            {
                                if (instr[i] == '/')
                                {
                                    state = 1;
                                }
                                outstr[o++] = instr[i];
                                break;
                            }
                    }
                }
                _canon = new string(outstr, 0, o);
                if (o > 1)
                {
                    o--;
                    i = _canon.IndexOf('/', 1);
                    if (i < 0)
                    {
                        _share = Runtime.Substring(_canon, 1);
                        Unc = "\\";
                    }
                    else
                    {
                        if (i == o)
                        {
                            _share = Runtime.Substring(_canon, 1, i);
                            Unc = "\\";
                        }
                        else
                        {
                            _share = Runtime.Substring(_canon, 1, i);
                            Unc = Runtime.Substring(_canon, i, outstr[o] == '/' ? o : o + 1);
                            Unc = Unc.Replace('/', '\\');
                        }
                    }
                }
                else
                {
                    _share = null;
                    Unc = "\\";
                }
            }
            return Unc;
        }

        /// <summary>Retrieve the hostname of the server for this SMB resource.</summary>
        /// <remarks>
        /// Retrieve the hostname of the server for this SMB resource. If this
        /// <code>SmbFile</code> references a workgroup, the name of the workgroup
        /// is returned. If this <code>SmbFile</code> refers to the root of this
        /// SMB network hierarchy, <code>null</code> is returned.
        /// </remarks>
        /// <returns>
        /// The server or workgroup name or <code>null</code> if this
        /// <code>SmbFile</code> refers to the root <code>smb://</code> resource.
        /// </returns>
        public string GetServer()
        {
            string str = Url.GetHost();
            if (str.Length == 0)
            {
                return null;
            }
            return str;
        }

        /// <summary>Returns type of of object this <tt>SmbFile</tt> represents.</summary>
        /// <remarks>Returns type of of object this <tt>SmbFile</tt> represents.</remarks>
        /// <returns>
        /// <tt>TYPE_FILESYSTEM, TYPE_WORKGROUP, TYPE_SERVER, TYPE_SHARE,
        /// TYPE_PRINTER, TYPE_NAMED_PIPE</tt>, or <tt>TYPE_COMM</tt>.
        /// </returns>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public new int GetType()
        {
            if (Type == 0)
            {
                if (GetUncPath0().Length > 1)
                {
                    Type = TypeFilesystem;
                }
                else
                {
                    throw new NotSupportedException("Unknown Type");
                }
            }
            return Type;
        }

        /// <summary>Returns the length of this <tt>SmbFile</tt> in bytes.</summary>
        /// <remarks>
        /// Returns the length of this <tt>SmbFile</tt> in bytes. If this object
        /// is a <tt>TYPE_SHARE</tt> the total capacity of the disk shared in
        /// bytes is returned. If this object is a directory or a type other than
        /// <tt>TYPE_SHARE</tt>, 0L is returned.
        /// </remarks>
        /// <returns>
        /// The length of the file in bytes or 0 if this
        /// <code>SmbFile</code> is not a file.
        /// </returns>
        /// <exception cref="SmbException">SmbException</exception>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public long Length()
        {
            if (_sizeExpiration > Runtime.CurrentTimeMillis())
            {
                return _size;
            }

            if (GetUncPath0().Length > 1)
            {
                IInfo info = QueryPath(GetUncPath0(),
                                       Trans2QueryPathInformationResponse.SMB_QUERY_FILE_STANDARD_INFO);
                _size = info.GetSize();
            }
            else
            {
                _size = 0L;
            }
            _sizeExpiration = Runtime.CurrentTimeMillis() + AttrExpirationPeriod;
            return _size;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal IInfo QueryPath(string path, int infoLevel)
        {
            Connect0();
            if (Log.Level >= 3)
            {
                Log.WriteLine("queryPath: " + path);
            }
            if (Tree.Session.transport.HasCapability(SmbConstants.CapNtSmbs))
            {
                Trans2QueryPathInformationResponse response
                    = new Trans2QueryPathInformationResponse(infoLevel);
                Send(new Trans2QueryPathInformation(path, infoLevel), response);
                return response.Info;
            }
            else
            {
                SmbComQueryInformationResponse response
                    = new SmbComQueryInformationResponse(Tree.Session.transport.Server.ServerTimeZone
                                                         * 1000
                                                         * 60L);
                Send(new SmbComQueryInformation(path), response);
                return response;
            }
        }

        /// <summary>
        /// Computes a hashCode for this file based on the URL string and IP
        /// address if the server.
        /// </summary>
        /// <remarks>
        /// Computes a hashCode for this file based on the URL string and IP
        /// address if the server. The hashing function uses the hashcode of the
        /// server address, the canonical representation of the URL, and does not
        /// compare authentication information. In essance, two
        /// <code>SmbFile</code> objects that refer to
        /// the same file should generate the same hashcode provided it is possible
        /// to make such a determination.
        /// </remarks>
        /// <returns>A hashcode for this abstract file</returns>
        /// <exception cref="SmbException">SmbException</exception>
        public override int GetHashCode()
        {
            int hash;
            try
            {
                hash = GetAddress().GetHashCode();
            }
            catch (UnknownHostException)
            {
                hash = GetServer().ToUpper(CultureInfo.InvariantCulture).GetHashCode();
            }
            GetUncPath0();
            return hash + _canon.ToUpper(CultureInfo.InvariantCulture).GetHashCode();
        }

        private bool PathNamesPossiblyEqual(string path1, string path2)
        {
            int p1;
            int p2;
            int l1;
            int l2;
            // if unsure return this method returns true
            p1 = path1.LastIndexOf('/');
            p2 = path2.LastIndexOf('/');
            l1 = path1.Length - p1;
            l2 = path2.Length - p2;
            // anything with dots voids comparison
            if (l1 > 1 && path1[p1 + 1] == '.')
            {
                return true;
            }
            if (l2 > 1 && path2[p2 + 1] == '.')
            {
                return true;
            }
            return l1 == l2 && path1.RegionMatches(true, p1, path2, p2, l1);
        }

        /// <summary>Tests to see if two <code>SmbFile</code> objects are equal.</summary>
        /// <remarks>
        /// Tests to see if two <code>SmbFile</code> objects are equal. Two
        /// SmbFile objects are equal when they reference the same SMB
        /// resource. More specifically, two <code>SmbFile</code> objects are
        /// equals if their server IP addresses are equal and the canonicalized
        /// representation of their URLs, minus authentication parameters, are
        /// case insensitivly and lexographically equal.
        /// <p/>
        /// For example, assuming the server <code>angus</code> resolves to the
        /// <code>192.168.1.15</code> IP address, the below URLs would result in
        /// <code>SmbFile</code>s that are equal.
        /// <p><blockquote><pre>
        /// smb://192.168.1.15/share/DIR/foo.txt
        /// smb://angus/share/data/../dir/foo.txt
        /// </pre></blockquote>
        /// </remarks>
        /// <param name="obj">Another <code>SmbFile</code> object to compare for equality</param>
        /// <returns>
        /// <code>true</code> if the two objects refer to the same SMB resource
        /// and <code>false</code> otherwise
        /// </returns>
        /// <exception cref="SmbException">SmbException</exception>
        public override bool Equals(object obj)
        {
            if (obj is SmbFile)
            {
                SmbFile f = (SmbFile)obj;
                bool ret;
                if (this == f)
                {
                    return true;
                }
                if (PathNamesPossiblyEqual(Url.AbsolutePath, f.Url.AbsolutePath))
                {
                    GetUncPath0();
                    f.GetUncPath0();
                    if (Runtime.EqualsIgnoreCase(_canon, f._canon))
                    {
                        try
                        {
                            ret = GetAddress().Equals(f.GetAddress());
                        }
                        catch (UnknownHostException)
                        {
                            ret = Runtime.EqualsIgnoreCase(GetServer(), f.GetServer());
                        }
                        return ret;
                    }
                }
            }
            return false;
        }

        /// <summary>Returns the string representation of this SmbFile object.</summary>
        /// <remarks>
        /// Returns the string representation of this SmbFile object. This will
        /// be the same as the URL used to construct this <code>SmbFile</code>.
        /// This method will return the same value
        /// as <code>getPath</code>.
        /// </remarks>
        /// <returns>The original URL representation of this SMB resource</returns>
        /// <exception cref="SmbException">SmbException</exception>
        public override string ToString()
        {
            return Url.ToString();
        }

        /// <summary>This URLConnection method just returns a new <tt>SmbFileInputStream</tt> created with this file.
        /// </summary>
        /// <remarks>This URLConnection method just returns a new <tt>SmbFileInputStream</tt> created with this file.
        /// </remarks>
        /// <exception cref="System.IO.IOException">thrown by <tt>SmbFileInputStream</tt> constructor
        /// </exception>
        public InputStream GetInputStream()
        {
            return new SmbFileInputStream(this);
        }
    }
}