using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using atomex.ViewModel;
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
        public bool IsBtcBased => _currency is BitcoinBasedConfig;

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
                        : outputs.ElementAt(0).DestinationAddress(_config.Network);

                    var totalOutputsSatoshi = outputs
                        .Aggregate((long)0, (sum, output) => sum + output.Value);

                    SelectedFromBalance = _config.SatoshiToCoin(totalOutputsSatoshi);
                });

            this.WhenAnyValue(vm => vm.FeeRate)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => !useDefaultFee)
                .SubscribeInMainThread((useDefaultFee) => { _ = UpdateFee(); });

            var outputs = _account.GetAvailableOutputsAsync()
                .WaitForResult()
                .Select(output => new OutputViewModel()
                {
                    Output = (BitcoinBasedTxOutput)output,
                    Config = _config,
                    IsSelected = true
                })
                .ToList();

            SelectFromViewModel = new SelectOutputsViewModel(
                outputs: outputs,
                account: _account,
                config: _config,
                navigationService: _navigationService,
                tab: TabNavigation.Portfolio)
            {
                ConfirmAction = ConfirmOutputs
            };

            SelectToViewModel = new SelectAddressViewModel(
                account: _app.Account,
                currency: _currency,
                navigationService: _navigationService,
                tab: TabNavigation.Portfolio)
            {
                ConfirmAction = ConfirmToAddress
            };
        }

        private BitcoinBasedConfig _config => (BitcoinBasedConfig)_currency;

        [Reactive] private IEnumerable<BitcoinBasedTxOutput> _outputs { get; set; }

        [Reactive] public decimal FeeRate { get; set; }
        public string FeeRateCode => "sat/byte";

        private BitcoinBasedAccount _account => _app.Account.GetCurrencyAccount<BitcoinBasedAccount>(_currency.Name);

        protected void ConfirmOutputs(SelectOutputsViewModel selectOutputsViewModel, IEnumerable<BitcoinBasedTxOutput> outputs)
        {
            _outputs = new ObservableCollection<BitcoinBasedTxOutput>(outputs);

            switch (selectOutputsViewModel?.SelectAddressFrom)
            {
                case SelectAddressFrom.Init:
                    _navigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.Change:
                    _navigationService?.ClosePage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.InitSearch:
                    _navigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
                    _navigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    break;

                case SelectAddressFrom.ChangeSearch:
                    _navigationService?.RemovePreviousPage(TabNavigation.Portfolio);
                    _navigationService?.ClosePage(TabNavigation.Portfolio);
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
                    FeeRate = await _config.GetFeeRateAsync();

                    if (To == null)
                        return;

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeRateAsync(
                        availableOutputs: _outputs,
                        to: To,
                        amount: Amount,
                        feeRate: FeeRate,
                        account: _account);

                    if (transactionParams == null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: AppResources.InsufficientFunds);

                        return;
                    }

                    var feeVal = _config.SatoshiToCoin((long)transactionParams.FeeInSatoshi);
                    SetFeeFromString(feeVal.ToString());
                }
                else
                {
                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: _outputs,
                        to: To,
                        amount: Amount,
                        fee: Fee,
                        account: _account);

                    if (transactionParams == null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: AppResources.InsufficientFunds);

                        return;
                    }

                    var minimumFeeInSatoshi = _config.GetMinimumFee((int)transactionParams.Size);
                    var minimumFee = _config.SatoshiToCoin(minimumFeeInSatoshi);

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
                    account: _account);

                if (transactionParams == null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: AppResources.InsufficientFunds);

                    return;
                }

                var minimumFeeInSatoshi = _config.GetMinimumFee((int)transactionParams.Size);
                var minimumFee = _config.SatoshiToCoin(minimumFeeInSatoshi);

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
                    if (_outputs.Count() == 0)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: AppResources.InsufficientFunds);
                        SetAmountFromString("0");

                        return;
                    }

                    FeeRate = await _config.GetFeeRateAsync();

                    var maxAmountEstimation = await _account.EstimateMaxAmountToSendAsync(
                        outputs: _outputs,
                        to: To,
                        fee: null,
                        feeRate: FeeRate);

                    if (maxAmountEstimation.Amount > 0)
                    {
                        SetAmountFromString(maxAmountEstimation.Amount.ToString());

                        return;
                    }

                    var fee = maxAmountEstimation.Fee;
                    SetFeeFromString(fee.ToString());
                }
                else
                {
                    var availableInSatoshi = _outputs.Sum(o => o.Value);
                    var feeInSatoshi = _config.CoinToSatoshi(Fee);
                    var maxAmountInSatoshi = Math.Max(availableInSatoshi - feeInSatoshi, 0);
                    var maxAmount = _config.SatoshiToCoin(maxAmountInSatoshi);

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: _outputs,
                        to: To,
                        amount: maxAmount,
                        fee: Fee,
                        account: _account);

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
                        SetAmountFromString(maxAmount.ToString());
                    }
                    else
                    {
                        var minimumFeeInSatoshi = _config.GetMinimumFee((int)transactionParams.Size);
                        var minimumFee = _config.SatoshiToCoin(minimumFeeInSatoshi);

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
            return _account.SendAsync(
                from: _outputs,
                to: To,
                amount: AmountToSend,
                fee: Fee,
                dustUsagePolicy: DustUsagePolicy.AddToFee,
                cancellationToken: cancellationToken);
        }

        protected async override void FromClick()
        {
            var outputs = (await _account.GetAvailableOutputsAsync())
                .Select(output => new OutputViewModel()
                {
                    Output = (BitcoinBasedTxOutput)output,
                    Config = _config,
                    IsSelected = _outputs.Any(o =>
                        output.TxId == o.TxId &&
                        output.Index == o.Index)

                })
                .ToList();

            SelectFromViewModel = new SelectOutputsViewModel(
                outputs: outputs,
                account: _account,
                config: _config,
                navigationService: _navigationService)
            {
                ConfirmAction = ConfirmOutputs
            };

            var selectFromViewModel = SelectFromViewModel as SelectOutputsViewModel;
            selectFromViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            _navigationService?.ShowPage(new SelectOutputsPage(SelectFromViewModel as SelectOutputsViewModel), TabNavigation.Portfolio);
        }

        protected override void ToClick()
        {
            SelectToViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            _navigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
        }
    }
}