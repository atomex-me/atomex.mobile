using System;
using System.Linq;
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
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.Message;

namespace atomex.ViewModel.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        private ReactiveCommand<MaxAmountEstimation, MaxAmountEstimation> CheckAmountCommand;

        [Reactive] public bool HasTokens { get; set; }
        [Reactive] public bool HasActiveSwaps { get; set; }

        public TezosSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
            CheckAmountCommand = ReactiveCommand.Create<MaxAmountEstimation, MaxAmountEstimation>(estimation => estimation);

            CheckAmountCommand.Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(estimation => CheckAmount(estimation));

            SelectFromViewModel = new SelectAddressViewModel(App.Account, Currency, Navigation, SelectAddressMode.SendFrom)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(App.Account, Currency, Navigation)
            {
                ConfirmAction = ConfirmToAddress
            };

            if (Currency.Name == "XTZ")
            {
                CheckTokensAsync();
                CheckActiveSwapsAsync();
            }
        }

        private async void CheckTokensAsync()
        {
            var account = App.Account
                .GetCurrencyAccount<TezosAccount>(Currency.Name);

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

        protected override async Task UpdateAmount()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<TezosAccount>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        to: To,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    SetFeeFromString(maxAmountEstimation.Fee.ToString());

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update amount error", Currency?.Description);
            }
        }

        protected override async Task UpdateFee()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = App.Account
                        .GetCurrencyAccount<TezosAccount>(Currency.Name);

                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: From,
                            to: To,
                            type: BlockchainTransactionType.Output,
                            reserve: false);

                    CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update fee error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<TezosAccount>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        to: To,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    SetFeeFromString(maxAmountEstimation.Fee.ToString());

                if (maxAmountEstimation.Error != null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: maxAmountEstimation.Error.Description);
                    SetAmountFromString("0");

                    return;
                }

                var (fa12TransferFee, _) = await App.Account
                   .GetCurrencyAccount<Fa12Account>("TZBTC")
                   .EstimateTransferFeeAsync(From);

                var maxAmount = UseDefaultFee
                    ? maxAmountEstimation.Amount
                    : maxAmountEstimation.Amount + maxAmountEstimation.Fee - Fee;

                RecommendedMaxAmount = HasActiveSwaps
                    ? Math.Max(maxAmount - maxAmountEstimation.Reserved, 0)
                    : HasTokens
                        ? Math.Max(maxAmount - fa12TransferFee, 0)
                        : maxAmount;

                var amount = maxAmount > 0
                    ? HasActiveSwaps
                        ? RecommendedMaxAmount
                        : maxAmount
                    : 0;

                SetAmountFromString(amount.ToString());

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        private async void CheckAmount(MaxAmountEstimation maxAmountEstimation)
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

            if (Amount + Fee > maxAmountEstimation.Amount + maxAmountEstimation.Fee)
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Amount,
                    text: AppResources.InsufficientFunds);
                
                return;
            }

            if (Fee < maxAmountEstimation.Fee)
            {
                ShowMessage(
                    messageType: MessageType.Error,
                    element: RelatedTo.Amount,
                    text: AppResources.LowFees);
                
                return;
            }

            var (fa12TransferFee, _) = await App.Account
                .GetCurrencyAccount<Fa12Account>("TZBTC")
                .EstimateTransferFeeAsync(From);

            var maxAmount = UseDefaultFee
                ? maxAmountEstimation.Amount
                : maxAmountEstimation.Amount + maxAmountEstimation.Fee - Fee;

            RecommendedMaxAmount = HasActiveSwaps
                ? Math.Max(maxAmount - maxAmountEstimation.Reserved, 0)
                : HasTokens
                    ? Math.Max(maxAmount - fa12TransferFee, 0)
                    : maxAmount;

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
            var account = App.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: AmountToSend,
                fee: Fee,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}