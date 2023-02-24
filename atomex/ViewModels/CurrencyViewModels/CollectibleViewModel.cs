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
        [Reactive] public IEnumerable<TezosTokenViewModel> Tokens { get; set; }

        [Reactive] public TezosTokenViewModel SelectedToken { get; set; }
        public string ContractAddress => Tokens.First().Contract.Address;
        public string Name => Tokens.First().Contract.Name ?? ContractAddress.TruncateAddress();
        [Reactive] public ImageSource TokenPreview { get; set; }

        protected ImageSource GetCollectiblePreview(string url)
        {
            var hasImageInCache = CacheHelper
                .HasCacheAsync(new Uri(url))
                .WaitForResult();

            if (hasImageInCache)
            {
                return new UriImageSource
                {
                    Uri = new Uri(url),
                    CachingEnabled = true,
                    CacheValidity = new TimeSpan(365, 0, 0, 0)
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
                    CacheValidity = new TimeSpan(365, 0, 0, 0)
                };
            }

            return null;
        }

        private void InitCollectiblePreview()
        {
            var url = ThumbsApi.GetTokenPreviewUrl(Tokens.First().Contract.Address,
                Tokens.First().TokenBalance.TokenId);

            Device.InvokeOnMainThreadAsync(() => TokenPreview = GetCollectiblePreview(url));
        }

        public int Amount => Tokens
            .Aggregate(0, (result, tokenViewModel) => result + decimal.ToInt32(tokenViewModel.TotalAmount));
        
        public CollectibleViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            this.WhenAnyValue(vm => vm.Tokens)
                .WhereNotNull()
                .Subscribe(_ => InitCollectiblePreview());

            this.WhenAnyValue(vm => vm.SelectedToken)
                .WhereNotNull()
                .SubscribeInMainThread(async token =>
                {
                    _navigationService?.ShowPage(new NftPage(token), TabNavigation.Portfolio);
                    token.IsOpenToken = true;
                    
                    await Task.Run(async () =>
                    {
                        await SelectedToken!.LoadTransfersAsync();
                        SelectedToken.LoadAddresses();
                    });

                    SelectedToken = null;
                });
        }
    }
}