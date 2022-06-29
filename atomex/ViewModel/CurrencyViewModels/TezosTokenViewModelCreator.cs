using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Wallet.Tezos;

namespace atomex.ViewModel.CurrencyViewModels
{
    public static class TezosTokenViewModelCreator
    {
        private static readonly ConcurrentDictionary<KeyValuePair<string, decimal>, TezosTokenViewModel> Instances =
            new();

        public static async Task<IEnumerable<TezosTokenViewModel>> CreateOrGet(
            IAtomexApp atomexApp,
            INavigationService navigationService,
            TokenContract contract)
        {
            var tezosAccount = atomexApp.Account.GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var tokenWalletAddresses = await tezosAccount
                .DataRepository
                .GetTezosTokenAddressesByContractAsync(contract.Address);

            var tokenGroups = tokenWalletAddresses
                .Where(walletAddress => !walletAddress.TokenBalance.IsNft)
                .GroupBy(walletAddress => walletAddress.TokenBalance.TokenId);

            var walletAddresses = tokenGroups
                .Select(walletAddressGroup =>
                    walletAddressGroup.Skip(1).Aggregate(walletAddressGroup.First(), (result, walletAddress) =>
                    {
                        result.TokenBalance.ParsedBalance = result.TokenBalance.GetTokenBalance() +
                                                            walletAddress.TokenBalance.GetTokenBalance();

                        return result;
                    }));

            var tokens = new List<TezosTokenViewModel>();

            foreach (var walletAddress in walletAddresses)
            {
                var kv = new KeyValuePair<string, decimal>(
                    contract.Address,
                    walletAddress.TokenBalance.TokenId);

                if (Instances.TryGetValue(kv, out var cachedTokenViewModel))
                {
                    tokens.Add(cachedTokenViewModel);
                    continue;
                }

                var tokenViewModel = new TezosTokenViewModel(
                    app: atomexApp,
                    navigationService: navigationService,
                    tokenBalance: walletAddress.TokenBalance,
                    contract: contract,
                    address: walletAddress.Address);

                tokenViewModel.UpdateQuotesInBaseCurrency(atomexApp.QuotesProvider);
                tokenViewModel.SubscribeToUpdates();
                Instances.TryAdd(kv, tokenViewModel);
                tokens.Add(tokenViewModel);
            }

            return tokens.Where(token => !token.TokenBalance.IsNft);
        }

        public static void Reset()
        {
            foreach (var tokenViewModel in Instances.Values)
            {
                tokenViewModel.Dispose();
            }

            Instances.Clear();
        }
    }
}
