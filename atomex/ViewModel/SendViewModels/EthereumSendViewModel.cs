using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Common;
using atomex.Models;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.Wallet.Abstract;
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

        private ReactiveCommand<MaxAmountEstimation, MaxAmountEstimation> CheckAmountCommand;

        public virtual string TotalFeeCurrencyCode => CurrencyCode;
        public string GasPriceCode => "GWEI";
        public string GasLimitCode => "GAS";

        public int GasLimit => decimal.ToInt32(Currency.GetDefaultFee());
        [Reactive] public int GasPrice { get; set; }
        [Reactive] public decimal TotalFee { get; set; }
        [Reactive] public bool HasTokens { get; set; }
        [Reactive] public bool HasActiveSwaps { get; set; }

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

        public EthereumSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
            Fee = Currency.GetFeeAmount(GasLimit, GasPrice);

            var updateGasPriceCommand = ReactiveCommand.CreateFromTask(UpdateGasPrice);

            this.WhenAnyValue(vm => vm.GasPrice)
                .SubscribeInMainThread(_ => Message.Text = string.Empty);

            this.WhenAnyValue(vm => vm.GasPrice)
                .SubscribeInMainThread(gasPrice =>
                {
                    GasPriceString = gasPrice.ToString(CultureInfo.InvariantCulture);
                    this.RaisePropertyChanged(nameof(GasPriceString));
                });

            this.WhenAnyValue(vm => vm.GasPrice)
                .Where(_ => !string.IsNullOrEmpty(From))
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateGasPriceCommand);

            this.WhenAnyValue(vm => vm.GasPrice)
               .Where(_ => !string.IsNullOrEmpty(From))
               .SubscribeInMainThread(_ =>
               {
                   Fee = Currency.GetFeeAmount(GasLimit, GasPrice);
               });

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee,
                    (amount, fee) => Currency.IsToken ? amount : amount + fee
                )
                .Select(totalAmount => totalAmount.ToString(CultureInfo.InvariantCulture))
                .ToPropertyExInMainThread(this, vm => vm.TotalAmountString);

            CheckAmountCommand = ReactiveCommand.Create<MaxAmountEstimation, MaxAmountEstimation>(estimation => estimation);

            CheckAmountCommand.Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(estimation => CheckAmount(estimation));

            SelectFromViewModel = new SelectAddressViewModel(
                account: App.Account,
                currency: Currency,
                navigation: Navigation,
                mode: SelectAddressMode.SendFrom)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(
                account: App.Account,
                currency: Currency,
                navigation: Navigation)
            {
                ConfirmAction = ConfirmToAddress
            };

            if (Currency.Name == "ETH")
            {
                CheckTokensAsync();
                CheckActiveSwapsAsync();
            }
        }

        protected override async Task FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;
            selectFromViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            await Navigation.PushAsync(new SelectAddressPage(selectFromViewModel));
        }

        protected override async Task ToClick()
        {
            SelectToViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            await Navigation.PushAsync(new SelectAddressPage(SelectToViewModel));
        }

        private async void CheckTokensAsync()
        {
            var account = App.Account
                .GetCurrencyAccount<EthereumAccount>(Currency.Name);

            var unpsentTokens = await account
                .GetUnspentTokenAddressesAsync()
                .ConfigureAwait(false);

            await Device.InvokeOnMainThreadAsync(() =>
            {
                HasTokens = unpsentTokens.Any(); // todo: use tokens count to calculate reserved fee more accurately
            }).ConfigureAwait(false);
        }

        private async void CheckActiveSwapsAsync()
        {
            var activeSwaps = (await App.Account
                .GetSwapsAsync()
                .ConfigureAwait(false))
                .Where(s => s.IsActive && (s.SoldCurrency == Currency.Name || s.PurchasedCurrency == Currency.Name));

            await Device.InvokeOnMainThreadAsync(() =>
            {
                HasActiveSwaps = activeSwaps.Any(); // todo: use swaps count to calculate reserved fee more accurately
            }).ConfigureAwait(false);
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

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
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

                    CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
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
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: maxAmountEstimation.Error.Description,
                        tooltipText: maxAmountEstimation.Error.Details);
                    SetAmountFromString("0");

                    return;
                }

                var erc20Config = App.Account.Currencies.Get<Erc20Config>("USDT");
                var erc20TransferFee = erc20Config.GetFeeAmount(erc20Config.TransferGasLimit, GasPrice);

                RecommendedMaxAmount = HasActiveSwaps
                    ? Math.Max(maxAmountEstimation.Amount - maxAmountEstimation.Reserved, 0)
                    : HasTokens
                        ? Math.Max(maxAmountEstimation.Amount - erc20TransferFee, 0)
                        : maxAmountEstimation.Amount;

                // force to use RecommendedMaxAmount in case when there are active swaps
                var amount = maxAmountEstimation.Amount > 0
                    ? HasActiveSwaps
                        ? RecommendedMaxAmount
                        : maxAmountEstimation.Amount
                    : 0;
                SetAmountFromString(amount.ToString());

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        private void CheckAmount(MaxAmountEstimation maxAmountEstimation)
        {
            if (maxAmountEstimation.Error != null)
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Amount,
                    text: maxAmountEstimation.Error.Description,
                    tooltipText: maxAmountEstimation.Error.Details);

                return;
            }

            if (Amount > maxAmountEstimation.Amount)
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Amount,
                    text: AppResources.InsufficientFunds);

                return;
            }

            var erc20Config = App.Account.Currencies.Get<Erc20Config>("USDT");
            var erc20TransferFee = erc20Config.GetFeeAmount(erc20Config.TransferGasLimit, GasPrice);

            RecommendedMaxAmount = HasActiveSwaps
                ? Math.Max(maxAmountEstimation.Amount - maxAmountEstimation.Reserved, 0)
                : HasTokens
                    ? Math.Max(maxAmountEstimation.Amount - erc20TransferFee, 0)
                    : maxAmountEstimation.Amount;

            if (HasActiveSwaps && Amount > RecommendedMaxAmount)
            {
                SetRecommededAmountWarning(
                    MessageType.Error,
                    RelatedTo.Amount,
                    string.Format(
                        AppResources.MaxAmountToSendWithActiveSwaps,
                        RecommendedMaxAmount,
                        Currency.Name),
                    string.Format(
                        AppResources.MaxAmountToSendWithActiveSwapsDetails,
                        RecommendedMaxAmount,
                        Currency.Name));
                ShowAdditionalConfirmation = false;

                return;
            }
            
            if (HasActiveSwaps && Amount == RecommendedMaxAmount)
            {
                SetRecommededAmountWarning(
                    MessageType.Warning,
                    RelatedTo.Amount,
                    string.Format(
                        AppResources.MaxAmountToSendWithActiveSwaps,
                        RecommendedMaxAmount,
                        Currency.Name),
                    string.Format(
                        AppResources.MaxAmountToSendWithActiveSwapsDetails,
                        RecommendedMaxAmount,
                        Currency.Name));
                ShowAdditionalConfirmation = false;

                return;
            }

            if (!HasActiveSwaps && HasTokens && Amount >= RecommendedMaxAmount)
            {
                SetRecommededAmountWarning(
                    MessageType.Regular,
                    RelatedTo.Amount,
                    string.Format(
                        AppResources.MaxAmountToSendRecommendation,
                        RecommendedMaxAmount,
                        Currency.Name),
                    string.Format(
                        AppResources.MaxAmountToSendRecommendationDetails,
                        RecommendedMaxAmount,
                        Currency.Name));
                ShowAdditionalConfirmation = true;

                return;
            }

            if (!HasActiveSwaps)
            {
                SetRecommededAmountWarning(
                    MessageType.Regular,
                    RelatedTo.Amount,
                    null,
                    null);
                ShowAdditionalConfirmation = false;
            }
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = App.Account
                .GetCurrencyAccount<EthereumAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: AmountToSend,
                gasLimit: GasLimit,
                gasPrice: GasPrice,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}

