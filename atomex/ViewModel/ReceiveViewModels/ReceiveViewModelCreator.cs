using System;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace atomex.ViewModel.ReceiveViewModels
{
    public static class ReceiveViewModelCreator
    {
        public static ReceiveViewModel CreateViewModel(
            CurrencyViewModel currencyViewModel)
        {
            return currencyViewModel.Currency switch
            {
                BitcoinBasedConfig _ => new ReceiveViewModel(currencyViewModel),
                Erc20Config _ => new ReceiveViewModel(currencyViewModel),
                EthereumConfig _ => new EthereumReceiveViewModel(currencyViewModel),
                Fa2Config _ => new ReceiveViewModel(currencyViewModel),
                Fa12Config _ => new ReceiveViewModel(currencyViewModel),
                TezosConfig _ => new TezosReceiveViewModel(currencyViewModel),
                _ => throw new NotSupportedException($"Can't create receive view model for {currencyViewModel.Currency.Name}. This currency is not supported."),
            };
        }
    }
}
