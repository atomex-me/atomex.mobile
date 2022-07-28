using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos;
using atomex.Common;
using Atomex.Common;
using atomex.Views.Collectibles;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xamarin.Forms;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class CollectibleViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private readonly INavigationService _navigationService;
        public IEnumerable<TezosTokenViewModel> Tokens { get; set; }

        [Reactive] public TezosTokenViewModel SelectedToken { get; set; }
        public string Name => Tokens.First().Contract.Name ?? Tokens.First().Contract.Address;

        public UriImageSource PreviewUrl
        {
            get
            {
                var url = ThumbsApi.GetCollectiblePreviewUrl(Tokens.First().Contract.Address,
                    Tokens.First().TokenBalance.TokenId);

                var hasImageInCache = CacheHelper
                    .HasCacheAsync(new Uri(url))
                    .WaitForResult();

                if (hasImageInCache)
                {
                    return new UriImageSource
                    {
                        Uri = new Uri(url),
                        CachingEnabled = true,
                        CacheValidity = new TimeSpan(5, 0, 0, 0)
                    };
                }

                var downloaded = CacheHelper
                    .SaveToCacheAsync(new Uri(url))
                    .WaitForResult();

                if (downloaded)
                {
                    return new UriImageSource
                    {
                        Uri = new Uri(url),
                        CachingEnabled = true,
                        CacheValidity = new TimeSpan(5, 0, 0, 0)
                    };
                }

                return null;
            }
        }

        public int Amount => Tokens
            .Aggregate(0, (result, tokenViewModel) => result + decimal.ToInt32(tokenViewModel.TotalAmount));


        public CollectibleViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            this.WhenAnyValue(vm => vm.SelectedToken)
                .WhereNotNull()
                .SubscribeInMainThread(async token =>
                {
                    _navigationService?.ShowPage(new NftPage(token), TabNavigation.Portfolio);

                    await Task.Run(async () =>
                    {
                        await SelectedToken.LoadTransfers();
                        SelectedToken.LoadAddresses();
                    });

                    SelectedToken = null;
                });
        }
    }
}