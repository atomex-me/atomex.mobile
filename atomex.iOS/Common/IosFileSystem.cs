using System;
using System.IO;
using Atomex.Common;

namespace atomex.Common.FileSystem
{
    public class IosFileSystem : IFileSystem
    {
        public IosFileSystem()
        {
        }

        public string PathToDocuments =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library");

        public string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public string AssetsDirectory => BaseDirectory;

        public string ToFullPath(string path) =>
            Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(BaseDirectory + path);

        public Stream GetResourceStream(string path) =>
            new FileStream(path, FileMode.Open, FileAccess.Read);
    }
}