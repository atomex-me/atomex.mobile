﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace atomex.ViewModel.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        public bool IsBtcBased => Currency is BitcoinBasedConfig;

        public BitcoinBasedSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
            this.WhenAnyValue(vm => vm.Outputs)
                .WhereNotNull()
                .Subscribe(outputs =>
                {
                    From = outputs.ToList().Count != 1
                        ? $"{outputs.ToList().Count} outputs"
                        : outputs.ElementAt(0).DestinationAddress(Config.Network);

                    var totalOutputsSatoshi = outputs
                        .Aggregate((long)0, (sum, output) => sum + output.Value);

                    SelectedAmount = Config.SatoshiToCoin(totalOutputsSatoshi);
                });

            var outputs = Account.GetAvailableOutputsAsync()
                .WaitForResult()
                .Select(output => new OutputViewModel()
                {
                    Output = (BitcoinBasedTxOutput)output,
                    Config = Config,
                    IsSelected = true
                })
                .ToList();

            SelectFromViewModel = new SelectOutputsViewModel(outputs, Account, Config)
            {
                ConfirmAction = ConfirmOutputs
            };

            SelectToViewModel = new SelectAddressViewModel(App.Account, Currency)
            {
                ConfirmAction = ConfirmToAddress,
                ScanAction = ScanPage,
                ScanResultAction = ScanResult
            };
        }

        private BitcoinBasedConfig Config => (BitcoinBasedConfig)Currency;

        [Reactive] private IEnumerable<BitcoinBasedTxOutput> Outputs { get; set; }

        [Reactive] public decimal FeeRate { get; set; }

        private BitcoinBasedAccount Account => App.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

        protected async void ConfirmOutputs(IEnumerable<BitcoinBasedTxOutput> outputs)
        {
            Outputs = new ObservableCollection<BitcoinBasedTxOutput>(outputs);
            await Navigation.PushAsync(new ToAddressPage(SelectToViewModel));
        }

        protected async void ChangeOutputs(IEnumerable<BitcoinBasedTxOutput> outputs)
        {
            Outputs = new ObservableCollection<BitcoinBasedTxOutput>(outputs);
            await Navigation.PopAsync();
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                if (Outputs == null)
                    return;

                if (UseDefaultFee)
                {
                    FeeRate = await Config.GetFeeRateAsync();

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeRateAsync(
                        availableOutputs: Outputs,
                        to: To,
                        amount: Amount,
                        feeRate: FeeRate,
                        account: Account);

                    if (transactionParams == null)
                    {
                        Warning = AppResources.InsufficientFunds;
                        return;
                    }

                    var feeVal = Config.SatoshiToCoin((long)transactionParams.FeeInSatoshi);
                    Fee = feeVal;
                }
                else
                {
                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: To,
                        amount: Amount,
                        fee: Fee,
                        account: Account);

                    if (transactionParams == null)
                    {
                        Warning = AppResources.InsufficientFunds;
                        return;
                    }

                    var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                    var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                    if (Fee < minimumFee)
                        Warning = AppResources.LowFees;

                    FeeRate = transactionParams.FeeRate;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Update Amount error");
            }
        }

        protected override async Task UpdateFee()
        {
            try
            {
                if (Outputs == null)
                    return;

                var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                    availableOutputs: Outputs,
                    to: To,
                    amount: Amount,
                    fee: Fee,
                    account: Account);

                if (transactionParams == null)
                {
                    Warning = AppResources.InsufficientFunds;
                    return;
                }

                var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                if (Fee < minimumFee)
                    Warning = AppResources.LowFees;

                FeeRate = transactionParams.FeeRate;

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Update Fee error");
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                if (Outputs == null)
                    return;

                if (UseDefaultFee)
                {
                    if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    FeeRate = await Config.GetFeeRateAsync();

                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromOutputs(Outputs),
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: 0,
                        feePrice: FeeRate,
                        reserve: false);

                    if (maxAmountEstimation.Amount > 0)
                    {
                        Amount = maxAmountEstimation.Amount;
                        AmountString = Amount.ToString();
                        this.RaisePropertyChanged(nameof(AmountString));

                        return;
                    }

                    Fee = maxAmountEstimation.Fee;
                }
                else
                {
                    var availableInSatoshi = Outputs.Sum(o => o.Value);
                    var feeInSatoshi = Config.CoinToSatoshi(Fee);
                    var maxAmountInSatoshi = Math.Max(availableInSatoshi - feeInSatoshi, 0);
                    var maxAmount = Config.SatoshiToCoin(maxAmountInSatoshi);

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: To,
                        amount: maxAmount,
                        fee: Fee,
                        account: Account);

                    if (transactionParams == null)
                    {
                        Warning = AppResources.InsufficientFunds;
                        Amount = 0;
                        AmountString = Amount.ToString();
                        this.RaisePropertyChanged(nameof(AmountString));

                        return;
                    }

                    if (Amount != maxAmount)
                    {
                        Amount = maxAmount;
                    }
                    else
                    {
                        var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                        var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                        if (Fee < minimumFee)
                            Warning = AppResources.LowFees;
                    }

                    AmountString = Amount.ToString();                        

                    FeeRate = transactionParams.FeeRate;
                }

                this.RaisePropertyChanged(nameof(AmountString));
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "Max Click error");
            }
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            return Account.SendAsync(
                from: Outputs,
                to: To,
                amount: Amount,
                fee: Fee,
                dustUsagePolicy: DustUsagePolicy.AddToFee,
                cancellationToken: cancellationToken);
        }

        protected async override Task FromClick()
        {
            var outputs = (await Account.GetAvailableOutputsAsync())
                .Select(output => new OutputViewModel()
                {
                    Output = (BitcoinBasedTxOutput)output,
                    Config = Config,
                    IsSelected = Outputs.Any(o =>
                        output.TxId == o.TxId &&
                        output.Index == o.Index)

                })
                .ToList();

            SelectFromViewModel = new SelectOutputsViewModel(outputs, Account, Config)
            {
                ConfirmAction = ChangeOutputs
            };

            await Navigation.PushAsync(new OutputsListPage(SelectFromViewModel as SelectOutputsViewModel));
        }

        protected async override Task ToClick()
        {
            SelectToViewModel.ConfirmAction = ChangeToAddress;
            await Navigation.PushAsync(new ToAddressPage(SelectToViewModel));
        }
    }
}