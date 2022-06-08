﻿using System;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace atomex.ViewModel.SendViewModels
{
    public static class SendViewModelCreator
    {
        public static SendViewModel CreateViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel,
            INavigationService navigationService)
        {
            return currencyViewModel.Currency switch
            {
                BitcoinBasedConfig _ => new BitcoinBasedSendViewModel(app, currencyViewModel, navigationService),
                Erc20Config _ => new Erc20SendViewModel(app, currencyViewModel, navigationService),
                EthereumConfig _ => new EthereumSendViewModel(app, currencyViewModel, navigationService),
                Fa12Config _ => new Fa12SendViewModel(app, currencyViewModel, navigationService),
                TezosConfig _ => new TezosSendViewModel(app, currencyViewModel, navigationService),
                _ => throw new NotSupportedException($"Can't create send view model for {currencyViewModel.Currency.Name}. This currency is not supported."),
            };
        }
    }
}
