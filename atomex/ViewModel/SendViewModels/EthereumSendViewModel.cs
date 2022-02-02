﻿using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.Wallet.Ethereum;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        public bool IsEthBased => Currency is EthereumConfig || Currency is Erc20Config;

        public EthereumSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
            Fee = Currency.GetFeeAmount(GasLimit, GasPrice);

            var updateGasPriceCommand = ReactiveCommand.CreateFromTask(UpdateGasPrice);

            this.WhenAnyValue(vm => vm.GasPrice)
                .Subscribe(_ => Warning = string.Empty);

            this.WhenAnyValue(vm => vm.GasPrice)
                .Subscribe(gasPrice => GasPriceString = gasPrice.ToString(CultureInfo.InvariantCulture));

            this.WhenAnyValue(vm => vm.GasPrice)
                .Where(_ => !string.IsNullOrEmpty(From))
                .Select(_ => Unit.Default)
                .InvokeCommand(updateGasPriceCommand);

            this.WhenAnyValue(vm => vm.GasPrice)
                .Select(_ => Unit.Default)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.GasPrice)
               .Where(_ => !string.IsNullOrEmpty(From))
               .Subscribe(_ =>
               {
                   Fee = Currency.GetFeeAmount(GasLimit, GasPrice);
                   OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
               });

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee,
                    (amount, fee) => Currency.IsToken ? amount : amount + fee
                )
                .Select(totalAmount => totalAmount.ToString(CultureInfo.InvariantCulture))
                .ToPropertyEx(this, vm => vm.TotalAmountString);

            SelectFromViewModel = new SelectAddressViewModel(App.Account, Currency, currencyViewModel?.Navigation, true)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(App.Account, Currency, currencyViewModel?.Navigation)
            {
                ConfirmAction = ConfirmToAddress
            };
        }

        public virtual string TotalFeeCurrencyCode => CurrencyCode;
        public string GasPriceCode => "GWEI";
        public string GasLimitCode => "GAS";

        public int GasLimit => decimal.ToInt32(Currency.GetDefaultFee());
        [Reactive] public int GasPrice { get; set; }
        [Reactive] public decimal TotalFee { get; set; }

        public string GasPriceString
        {
            get => GasPrice.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!int.TryParse(
                    s: temp,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.InvariantCulture,
                    result: out var gasPrice))
                {
                    GasPrice = 0;
                }
                else
                {
                    GasPrice = gasPrice;

                    if (GasPrice > int.MaxValue)
                        GasPrice = int.MaxValue;
                }

                Device.InvokeOnMainThreadAsync(() =>
                {
                    this.RaisePropertyChanged(nameof(GasPrice));
                });
            }
        }

        protected override async Task FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;
            await Navigation.PushAsync(new FromAddressPage(selectFromViewModel));
        }

        protected override async Task ToClick()
        {
            await Navigation.PushAsync(new ToAddressPage(SelectToViewModel));
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                    from: From,
                    type: BlockchainTransactionType.Output,
                    gasLimit: UseDefaultFee ? null : GasLimit,
                    gasPrice: UseDefaultFee ? null : GasPrice,
                    reserve: false);

                if (UseDefaultFee)
                {
                    if (maxAmountEstimation.Fee > 0)
                    {
                        GasPrice = decimal.ToInt32(Currency.GetFeePriceFromFeeAmount(maxAmountEstimation.Fee, GasLimit));
                    }
                    else
                    {
                        GasPrice = decimal.ToInt32(await Currency.GetDefaultFeePriceAsync());
                    }
                }

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Description;
                    return;
                }

                if (Amount > maxAmountEstimation.Amount)
                    Warning = AppResources.InsufficientFunds;

                OnQuotesUpdatedEventHandler(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update amount error", Currency?.Description);
            }
        }

        protected virtual async Task UpdateGasPrice()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = App.Account
                        .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                    // estimate max amount with new GasPrice
                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        gasLimit: GasLimit,
                        gasPrice: GasPrice,
                        reserve: false);

                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Description;
                        return;
                    }

                    if (Amount > maxAmountEstimation.Amount)
                        Warning = AppResources.InsufficientFunds;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update gas price error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        gasLimit: UseDefaultFee ? null : GasLimit,
                        gasPrice: UseDefaultFee ? null : GasPrice,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    GasPrice = decimal.ToInt32(Currency.GetFeePriceFromFeeAmount(maxAmountEstimation.Fee, GasLimit));

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Description;
                    Amount = 0;
                    AmountString = Amount.ToString();
                    this.RaisePropertyChanged(nameof(AmountString));

                    return;
                }

                Amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount
                    : 0;

                AmountString = Amount.ToString();
                this.RaisePropertyChanged(nameof(AmountString));
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = App.Account
                .GetCurrencyAccount<EthereumAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: Amount,
                gasLimit: GasLimit,
                gasPrice: GasPrice,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}

