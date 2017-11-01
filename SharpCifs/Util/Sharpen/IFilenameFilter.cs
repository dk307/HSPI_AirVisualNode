namespace SharpCifs.Util.Sharpen
{
    internal interface IFilenameFilter
    {
        bool Accept(FilePath dir, string name);
    }
}
