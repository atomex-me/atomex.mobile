using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Common;
using atomex.Resources;
using atomex.Views;
using Atomex;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Common;
using atomex.ViewModels.Abstract;
using atomex.ViewModels.CurrencyViewModels;
using Atomex.Wallet.BitcoinBased;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using static atomex.Models.Message;

namespace atomex.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        public bool IsBtcBased => Currency is BitcoinBasedConfig;

        public BitcoinBasedSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel,
            INavigationService navigationService)
            : base(app, currencyViewModel, navigationService)
        {
            this.WhenAnyValue(vm => vm._outputs)
                .WhereNotNull()
                .SubscribeInMainThread(outputs =>
                {
                    From = outputs.ToList().Count != 1
                        ? $"{outputs.ToList().Count} outputs"
                        : outputs.ElementAt(0).DestinationAddress(Config.Network);

                    var totalOutputsSatoshi = outputs
                        .Aggregate((long)0, (sum, output) => sum + output.Value);

                    SelectedFromBalance = Config.SatoshiToCoin(totalOutputsSatoshi);
                });

            this.WhenAnyValue(vm => vm.FeeRate)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => !useDefaultFee)
                .SubscribeInMainThread((useDefaultFee) => { _ = UpdateFee(); });

            var outputs = Account.GetAvailableOutputsAsync()
                .WaitForResult()
                .Select(output => new OutputViewModel()
                {
                    Output = (BitcoinBasedTxOutput)output,
                    Config = Config,
                    IsSelected = true
                })
                .ToList();

            SelectFromViewModel = new SelectOutputsViewModel(
                outputs: outputs,
                account: Account,
                config: Config,
                navigationService: NavigationService,
                tab: TabNavigation.Portfolio)
            {
                ConfirmAction = ConfirmOutputs
            };

            SelectToViewModel = new SelectAddressViewModel(
                account: App.Account,
                currency: Currency,
                navigationService: NavigationService,
                tab: TabNavigation.Portfolio)
            {
                ConfirmAction = ConfirmToAddress
            };
        }

        private BitcoinBasedConfig Config => (BitcoinBasedConfig)Currency;

        [Reactive] private IEnumerable<BitcoinBasedTxOutput> _outputs { get; set; }

        [Reactive] public decimal FeeRate { get; set; }
        public string FeeRateCode => "sat/byte";

        private BitcoinBasedAccount Account => App.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

        protected void ConfirmOutputs(SelectOutputsViewModel selectOutputsViewModel, IEnumerable<BitcoinBasedTxOutput> outputs)
        {
            _outputs = new ObservableCollection<BitcoinBasedTxOutput>(outputs);

            switch (selectOutputsViewModel?.SelectAddressFrom)
            {
                case SelectAddressFrom.Init:
                    NavigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.Change:
                    NavigationService?.ClosePage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.InitSearch:
                    NavigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
                    NavigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.ChangeSearch:
                    NavigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    NavigationService?.ClosePage(TabNavigation.Portfolio);
                    break;
            }
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                if (_outputs == null)
                    return;

                if (UseDefaultFee)
                {
                    FeeRate = await Config.GetFeeRateAsync();

                    if (To == null)
                        return;

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeRateAsync(
                        availableOutputs: _outputs,
                        to: To,
                        amount: Amount,
                        feeRate: FeeRate,
                        account: Account);

                    if (transactionParams == null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: AppResources.InsufficientFunds);

                        return;
                    }

                    var feeVal = Config.SatoshiToCoin((long)transactionParams.FeeInSatoshi);
                    SetFeeFromString(feeVal.ToString(CultureInfo.CurrentCulture));
                }
                else
                {
                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: _outputs,
                        to: To,
                        amount: Amount,
                        fee: Fee,
                        account: Account);

                    if (transactionParams == null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: AppResources.InsufficientFunds);

                        return;
                    }

                    var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                    var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                    if (Fee < minimumFee)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Fee,
                            text: AppResources.LowFees);
                    }

                    FeeRate = transactionParams.FeeRate;
                }
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
                if (_outputs == null)
                    return;

                var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                    availableOutputs: _outputs,
                    to: To,
                    amount: Amount,
                    fee: Fee,
                    account: Account);

                if (transactionParams == null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: AppResources.InsufficientFunds);

                    return;
                }

                var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                if (Fee < minimumFee)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Fee,
                        text: AppResources.LowFees);
                }

                FeeRate = transactionParams.FeeRate;
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
                if (_outputs == null)
                    return;

                if (UseDefaultFee)
                {
                    if (!_outputs.Any())
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: AppResources.InsufficientFunds);
                        SetAmountFromString("0");

                        return;
                    }

                    FeeRate = await Config.GetFeeRateAsync();

                    var maxAmountEstimation = await Account.EstimateMaxAmountToSendAsync(
                        outputs: _outputs,
                        to: To,
                        fee: null,
                        feeRate: FeeRate);

                    if (maxAmountEstimation.Amount > 0)
                    {
                        SetAmountFromString(maxAmountEstimation.Amount.ToString(CultureInfo.CurrentCulture));

                        return;
                    }

                    var fee = maxAmountEstimation.Fee;
                    SetFeeFromString(fee.ToString(CultureInfo.CurrentCulture));
                }
                else
                {
                    var availableInSatoshi = _outputs.Sum(o => o.Value);
                    var feeInSatoshi = Config.CoinToSatoshi(Fee);
                    var maxAmountInSatoshi = Math.Max(availableInSatoshi - feeInSatoshi, 0);
                    var maxAmount = Config.SatoshiToCoin(maxAmountInSatoshi);

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: _outputs,
                        to: To,
                        amount: maxAmount,
                        fee: Fee,
                        account: Account);

                    if (transactionParams == null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: AppResources.InsufficientFunds);
                        SetAmountFromString("0");

                        return;
                    }

                    if (Amount != maxAmount)
                    {
                        SetAmountFromString(maxAmount.ToString(CultureInfo.CurrentCulture));
                    }
                    else
                    {
                        var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                        var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                        if (Fee < minimumFee)
                        {
                            ShowMessage(
                                messageType: MessageType.Error,
                                element: RelatedTo.Fee,
                                text: AppResources.LowFees);
                        }
                    }

                    FeeRate = transactionParams.FeeRate;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Max Click error");
            }
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            return Account.SendAsync(
                from: _outputs,
                to: To,
                amount: AmountToSend,
                fee: Fee,
                dustUsagePolicy: DustUsagePolicy.AddToFee,
                cancellationToken: cancellationToken);
        }

        protected async override void FromClick()
        {
            var outputs = (await Account.GetAvailableOutputsAsync())
                .Select(output => new OutputViewModel()
                {
                    Output = (BitcoinBasedTxOutput)output,
                    Config = Config,
                    IsSelected = _outputs.Any(o =>
                        output.TxId == o.TxId &&
                        output.Index == o.Index)

                })
                .ToList();

            SelectFromViewModel = new SelectOutputsViewModel(
                outputs: outputs,
                account: Account,
                config: Config,
                navigationService: NavigationService)
            {
                ConfirmAction = ConfirmOutputs
            };

            var selectFromViewModel = SelectFromViewModel as SelectOutputsViewModel;
            
            if (selectFromViewModel == null) return;
            selectFromViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            NavigationService?.ShowPage(new SelectOutputsPage(SelectFromViewModel as SelectOutputsViewModel), TabNavigation.Portfolio);
        }

        protected override void ToClick()
        {
            SelectToViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            NavigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
        }
    }
}