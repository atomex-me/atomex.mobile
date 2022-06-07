using System;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosTokenViewModel : BaseViewModel
    {
        public TezosConfig TezosConfig { get; set; }
        public TokenBalance TokenBalance { get; set; }
        public TokenContract Contract { get; set; }
        public string Address { get; set; }
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

        private ICommand _openInBrowser;
        public ICommand OpenInBrowser => _openInBrowser ??= new Command(() =>
        {
            var assetUrl = AssetUrl;

            if (assetUrl != null && Uri.TryCreate(assetUrl, UriKind.Absolute, out var uri))
                Launcher.OpenAsync(new Uri(uri.ToString()));
            else
                Log.Error("Invalid uri for ipfs asset");
        });

        public bool IsIpfsAsset =>
            TokenBalance.ArtifactUri != null && ThumbsApi.HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string? AssetUrl => IsIpfsAsset
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
