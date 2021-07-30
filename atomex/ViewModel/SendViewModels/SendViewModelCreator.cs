using System;
using Atomex;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace atomex.ViewModel.SendViewModels
{
    public static class SendViewModelCreator
    {
        public static SendViewModel CreateViewModel(CurrencyViewModel currencyViewModel)
        {
            return currencyViewModel.Currency switch
            {
                BitcoinBasedConfig _ => (SendViewModel)new BitcoinBasedSendViewModel(currencyViewModel),
                Erc20Config _ => (SendViewModel)new Erc20SendViewModel(currencyViewModel),
                EthereumConfig _ => (SendViewModel)new EthereumSendViewModel(currencyViewModel),
                Fa2Config _ => (SendViewModel)new Fa2SendViewModel(currencyViewModel),
                Fa12Config _ => (SendViewModel)new Fa12SendViewModel(currencyViewModel),
                TezosConfig _ => (SendViewModel)new TezosSendViewModel(currencyViewModel),
                _ => throw new NotSupportedException($"Can't create send view model for {currencyViewModel.Currency.Name}. This currency is not supported."),
            };
        }
    }
}
