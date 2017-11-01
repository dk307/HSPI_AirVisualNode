using System.IO;

namespace SharpCifs.Util.Sharpen
{
    internal class BufferedReader : StreamReader
    {
        public BufferedReader(InputStreamReader r) : base(r.BaseStream)
        {
        }
    }
}
