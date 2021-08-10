using System;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace atomex.ViewModel.SendViewModels
{
    public static class SendViewModelCreator
    {
        public static SendViewModel CreateViewModel(IAtomexApp app, CurrencyViewModel currencyViewModel)
        {
            return currencyViewModel.Currency switch
            {
                BitcoinBasedConfig _ => (SendViewModel)new BitcoinBasedSendViewModel(app, currencyViewModel),
                Erc20Config _ => (SendViewModel)new Erc20SendViewModel(app, currencyViewModel),
                EthereumConfig _ => (SendViewModel)new EthereumSendViewModel(app, currencyViewModel),
                Fa12Config _ => (SendViewModel)new Fa12SendViewModel(app, currencyViewModel),
                TezosConfig _ => (SendViewModel)new TezosSendViewModel(app, currencyViewModel),
                _ => throw new NotSupportedException($"Can't create send view model for {currencyViewModel.Currency.Name}. This currency is not supported."),
            };
        }
    }
}
