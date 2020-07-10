using System;
using System.IO;
using Atomex.Common;

namespace atomex.Common.FileSystem
{
    public class AndroidFileSystem : IFileSystem
    {
        public AndroidFileSystem()
        {
        }

        public string PathToDocuments => Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        public string BaseDirectory => "";
        public string AssetsDirectory => "";

        public string ToFullPath(string path) => path;

        public Stream GetResourceStream(string path)
        {
            using var asset = Android.App.Application.Context.Assets.Open(path);

            var stream = new MemoryStream();
            asset.CopyTo(stream);
            stream.Flush();
            stream.Position = 0;

            return stream;
        }
    }
}
