using System;
using Atomex.Common;
using Atomex.Common.FileSystem;
using Xamarin.Forms;

namespace atomex.Common
{
    public static class FileSystemFactory
    {
        public static IFileSystem Create()
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS: return new IosFileSystem();
                case Device.Android: return new AndroidFileSystem();
                default: throw new NotSupportedException("Platform not supported");
            }
        }
    }
}