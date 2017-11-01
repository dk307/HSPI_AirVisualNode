namespace SharpCifs.Util.Sharpen
{
    internal class FileReader : InputStreamReader
    {
        //public FileReader (FilePath f) : base(f.GetPath ())
        //{
        //}
        //path -> fileStream
        public FileReader(InputStream s) : base(s)
        {
        }
    }
}
