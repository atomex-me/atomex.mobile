using System;
using atomex.Common;
using Atomex.Common;
using ReactiveUI;
using Xamarin.Forms;

namespace atomex.ViewModels
{
    public class BakerViewModel : BaseViewModel
    {
        public string LogoUrl { get; set; }
        public ImageSource Logo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Fee { get; set; }
        public decimal Roi { get; set; }
        public decimal MinDelegation { get; set; }
        public decimal StakingAvailable { get; set; }
        public bool IsCurrentlyActive { get; set; }

        public bool IsFull => StakingAvailable <= 0;
        public bool IsMinDelegation => MinDelegation > 0;

        public string FreeSpaceFormatted => StakingAvailable.ToString(StakingAvailable switch
        {
            > 999999999 => "0,,,.#B",
            > 999999 => "0,,.#M",
            > 999 => "0,.#K",
            < -999 => "0,.#K",
            _ => "0"
        });

        public BakerViewModel()
        {
            this.WhenAnyValue(vm => vm.LogoUrl)
                .WhereNotNull()
                .SubscribeInMainThread(url =>
                {
                    var hasImageInCache = CacheHelper
                        .HasCacheAsync(new Uri(url))
                        .WaitForResult();

                    if (hasImageInCache)
                    {
                        Logo = new UriImageSource
                        {
                            Uri = new Uri(url),
                            CachingEnabled = true,
                            CacheValidity = new TimeSpan(7, 0, 0, 0)
                        };
                    }

                    var downloaded = CacheHelper
                        .SaveToCacheAsync(new Uri(url))
                        .WaitForResult();

                    if (downloaded)
                    {
                        Logo = new UriImageSource
                        {
                            Uri = new Uri(url),
                            CachingEnabled = true,
                            CacheValidity = new TimeSpan(7, 0, 0, 0)
                        };
                    }
                });
        }
    }
}