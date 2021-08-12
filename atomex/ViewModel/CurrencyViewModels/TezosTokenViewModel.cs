using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosTokenViewModel : BaseViewModel
    {
        public TezosConfig TezosConfig { get; set; }
        public TokenBalance TokenBalance { get; set; }
        public string Address { get; set; }

        public UriImageSource TokenPreview
        {
            get
            {
                foreach (var url in GetTokenPreviewUrls())
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

        public string Balance => TokenBalance.Balance != "1"
            ? $"{TokenBalance.GetTokenBalance().ToString(CultureInfo.InvariantCulture)} {TokenBalance.Symbol}"
            : "";

        private ICommand _openInBrowser;
        public ICommand OpenInBrowser => _openInBrowser ??= new Command(() =>
        {
            var assetUrl = AssetUrl;

            if (assetUrl != null && Uri.TryCreate(assetUrl, UriKind.Absolute, out var uri))
                Launcher.OpenAsync(new Uri(uri.ToString()));
            else
                Log.Error("Invalid uri for ipfs asset");
        });

        public bool IsIpfsAsset => TokenBalance.ArtifactUri != null && HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

        public IEnumerable<string> GetTokenPreviewUrls()
        {
            yield return $"https://d38roug276qjor.cloudfront.net/{TokenBalance.Contract}/{TokenBalance.TokenId}.png";

            if (TokenBalance.ArtifactUri != null && HasIpfsPrefix(TokenBalance.ArtifactUri))
                yield return $"https://api.dipdup.net/thumbnail/{RemoveIpfsPrefix(TokenBalance.ArtifactUri)}";

            yield return $"https://services.tzkt.io/v1/avatars/{TokenBalance.Contract}";
        }

        public static string RemovePrefix(string s, string prefix) =>
            s.StartsWith(prefix) ? s.Substring(prefix.Length) : s;

        public static string RemoveIpfsPrefix(string url) =>
            RemovePrefix(url, "ipfs://");

        public static bool HasIpfsPrefix(string url) =>
            url?.StartsWith("ipfs://") ?? false;
    }

    public class TezosTokenContractViewModel : BaseViewModel
    {
        public TokenContract Contract { get; set; }
        public string IconUrl => $"https://services.tzkt.io/v1/avatars/{Contract.Address}";
        public bool IsFa12 => Contract.GetContractType() == "FA12";
        public bool IsFa2 => Contract.GetContractType() == "FA2";

        private string _name;
        public string Name
        {
            get
            {
                if (_name != null)
                    return _name;

                _ = TryGetAliasAsync();

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
