using System;
using Atomex;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace atomex.ViewModel.ReceiveViewModels
{
    public static class ReceiveViewModelCreator
    {
        public static ReceiveViewModel CreateViewModel(
            CurrencyViewModel currencyViewModel)
        {
            switch (currencyViewModel.Currency)
            {

                case BitcoinBasedCurrency _:
                    return (ReceiveViewModel)new ReceiveViewModel(currencyViewModel);
                case FA12 _:
                    return (ReceiveViewModel)new ReceiveViewModel(currencyViewModel);
                case ERC20 _:
                    return (ReceiveViewModel)new ReceiveViewModel(currencyViewModel);
                case Ethereum _:
                    return (ReceiveViewModel)new EthereumReceiveViewModel(currencyViewModel);
                case Tezos _:
                    return (ReceiveViewModel)new ReceiveViewModel(currencyViewModel);
            }

            return null;
        }
    }
}
