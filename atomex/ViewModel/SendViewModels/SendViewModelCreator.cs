using System;
using Atomex;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace atomex.ViewModel.SendViewModels
{
    public static class SendViewModelCreator
    {
        public static SendViewModel CreateViewModel(
            CurrencyViewModel currencyViewModel)
        {
            switch (currencyViewModel.Currency)
            {

                case BitcoinBasedCurrency _:
                    return (SendViewModel)new BitcoinBasedSendViewModel(currencyViewModel);
                case FA12 _:
                    return (SendViewModel)new Fa12SendViewModel(currencyViewModel);
                case ERC20 _:
                    return (SendViewModel)new Erc20SendViewModel(currencyViewModel);
                case Ethereum _:
                    return (SendViewModel)new EthereumSendViewModel(currencyViewModel);
                case Tezos _:
                    return (SendViewModel)new TezosSendViewModel(currencyViewModel);
            }

            return null;
        }
    }
}
