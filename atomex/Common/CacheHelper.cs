using System;
using System.IO;
using System.Threading.Tasks;

using Xamarin.Forms;

using Atomex.Common;

namespace atomex.Common
{
    public static class CacheHelper
    {
        private const string CacheName = "ImageLoaderCache";

        public static async Task<bool> HasCacheAsync(Uri uri)
        {
            try
            {
                var key = Device.PlatformServices.GetHash(uri.AbsoluteUri);

                var store = Device.PlatformServices.GetUserStoreForApplication();

                var pathToCache = Path.Combine(CacheName, key);

                return await store
                    .GetFileExistsAsync(pathToCache)
                    .ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SaveToCacheAsync(Uri uri)
        {
            try
            {
                var key = Device.PlatformServices.GetHash(uri.AbsoluteUri);

                var store = Device.PlatformServices.GetUserStoreForApplication();

                var pathToCache = Path.Combine(CacheName, key);

                using var writerStream = await store
                    .OpenFileAsync(pathToCache, FileMode.Create, FileAccess.Write)
                    .ConfigureAwait(false);

                var response = await HttpHelper.HttpClient
                    .GetAsync(uri)
                    .ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content
                        .ReadAsStreamAsync()
                        .ConfigureAwait(false);

                    await responseStream
                        .CopyToAsync(writerStream, bufferSize: 16384)
                        .ConfigureAwait(false);

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}