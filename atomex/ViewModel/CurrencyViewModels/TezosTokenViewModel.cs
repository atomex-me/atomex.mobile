using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.ViewModel.SendViewModels;
using atomex.Views;
using atomex.Views.TezosTokens;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.TezosTokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosTokenViewModel : BaseViewModel
    {
        protected IAtomexApp _app { get; set; }
        protected INavigationService _navigationService { get; set; }

        public TezosConfig TezosConfig { get; set; }
        public TokenBalance TokenBalance { get; set; }
        public TokenContract Contract { get; set; }
        public string Address { get; set; }
        public bool IsConvertable => _app?.Account?.Currencies
            .Any(c => c is Fa12Config fa12 && fa12?.TokenContractAddress == Contract.Address) ?? false;
        [Reactive] public decimal BalanceInBase { get; set; }
        [Reactive] public decimal CurrentQuote { get; set; }

        private ThumbsApi ThumbsApi => new ThumbsApi(
            new ThumbsApiSettings
            {
                ThumbsApiUri = TezosConfig.ThumbsApiUri,
                IpfsGatewayUri = TezosConfig.IpfsGatewayUri,
                CatavaApiUri = TezosConfig.CatavaApiUri
            });

        public UriImageSource TokenPreview
        {
            get
            {
                foreach (var url in ThumbsApi.GetTokenPreviewUrls(TokenBalance.Contract, TokenBalance.ThumbnailUri,
                             TokenBalance.DisplayUri ?? TokenBalance.ArtifactUri))
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
                }
                return null;
            }
        }

        public decimal Balance => TokenBalance.Balance != "1"
            ? TokenBalance.GetTokenBalance()
            : 1;

        public string Symbol => TokenBalance.Symbol != ""
            ? TokenBalance.Symbol
            : "TOKEN";

        public TezosTokenViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
        }

        private ReactiveCommand<Unit, Unit> _tokenActionSheetCommand;
        public ReactiveCommand<Unit, Unit> TokenActionSheetCommand => _tokenActionSheetCommand ??= ReactiveCommand.Create(() =>
            _navigationService?.ShowBottomSheet(new TokenActionBottomSheet(this)));

        private ReactiveCommand<Unit, Unit> _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(OnReceiveClick);

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(OnSendClick);

        protected ReactiveCommand<Unit, Unit> _convertCurrencyCommand;
        public ReactiveCommand<Unit, Unit> ConvertCurrencyCommand => _convertCurrencyCommand ??= ReactiveCommand.Create(() =>
        {
            if (IsConvertable)
            {
                var currency = _app.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == Contract.Address);

                if (currency == null)
                    return; // TODO: msg to user

                _navigationService?.CloseBottomSheet();
                _navigationService?.GoToExchange(currency);
            }
        });

        protected virtual void OnReceiveClick()
        {
            _navigationService?.CloseBottomSheet();
            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                currency: TezosConfig,
                navigationService: _navigationService);
            _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(receiveViewModel));
        }

        protected virtual void OnSendClick()
        {
            if (Balance <= 0) return;
            _navigationService?.CloseBottomSheet();

            _navigationService?.SetInitiatedPage(TabNavigation.Portfolio);
            var sendViewModel = new TezosTokensSendViewModel(
                app: _app,
                navigationService: _navigationService,
                tokenContract: Contract.Address,
                tokenId: 0,
                tokenType: Contract.GetContractType(),
                tokenPreview: TokenPreview);
            
            _navigationService?.ShowPage(new SelectAddressPage(sendViewModel.SelectFromViewModel), TabNavigation.Portfolio);
        }

        private ICommand _closeActionSheetCommand;
        public ICommand CloseActionSheetCommand => _closeActionSheetCommand ??= new Command(() =>
            _navigationService?.CloseBottomSheet());

        private ReactiveCommand<Unit, Unit> _openInBrowser;
        public ReactiveCommand<Unit, Unit> OpenInBrowser => _openInBrowser ??= ReactiveCommand.Create(() =>
        {
            var assetUrl = AssetUrl;

            if (assetUrl != null && Uri.TryCreate(assetUrl, UriKind.Absolute, out var uri))
                Launcher.OpenAsync(new Uri(uri.ToString()));
            else
                Log.Error("Invalid uri for ipfs asset");
        });

        public bool IsIpfsAsset =>
            TokenBalance.ArtifactUri != null && ThumbsApi.HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{ThumbsApi.RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

    }

    public class TezosTokenContractViewModel : BaseViewModel
    {
        public TokenContract Contract { get; set; }
        public string IconUrl => $"https://services.tzkt.io/v1/avatars/{Contract.Address}";
        public bool IsFa12 => Contract.GetContractType() == "FA12";
        public bool IsFa2 => Contract.GetContractType() == "FA2";

        private bool _isTriedToGetFromTzkt = false;

        private string _name;
        public string Name
        {
            get
            {
                if (_name != null)
                    return _name;

                if (!_isTriedToGetFromTzkt)
                {
                    _isTriedToGetFromTzkt = true;
                    _ = TryGetAliasAsync();
                }

                _name = Contract.Name;
                return _name;
            }
        }

        private async Task TryGetAliasAsync()
        {
            try
            {
                var response = await HttpHelper.HttpClient
                    .GetAsync($"https://api.tzkt.io/v1/accounts/{Contract.Address}")
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return;

                var stringResponse = await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

                var alias = JsonConvert.DeserializeObject<JObject>(stringResponse)
                    ?["alias"]
                    ?.Value<string>();

                if (alias != null)
                    _name = alias;

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged(nameof(Name));
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Alias getting error.");
            }
        }
    }
}
