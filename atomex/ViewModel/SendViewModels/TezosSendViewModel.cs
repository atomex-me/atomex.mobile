using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Common;
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
            CurrencyViewModel currencyViewModel,
            INavigationService navigationService)
            : base(app, currencyViewModel, navigationService)
        {
            CheckAmountCommand = ReactiveCommand.Create<MaxAmountEstimation, MaxAmountEstimation>(estimation => estimation);

            CheckAmountCommand.Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(estimation => CheckAmount(estimation));

            SelectFromViewModel = new SelectAddressViewModel(_app.Account, _currency, _navigationService, SelectAddressMode.SendFrom)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(_app.Account, _currency, _navigationService)
            {
                ConfirmAction = ConfirmToAddress
            };

            if (_currency.Name == "XTZ")
            {
                CheckTokensAsync();
                CheckActiveSwapsAsync();
            }
        }

        private async void CheckTokensAsync()
        {
            var account = _app.Account
                .GetCurrencyAccount<TezosAccount>(_currency.Name);

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
            var activeSwaps = (await _app.Account
                .GetSwapsAsync()
                .ConfigureAwait(false))
                .Where(s => s.IsActive && (s.SoldCurrency == _currency.Name || s.PurchasedCurrency == _currency.Name));

            await Device.InvokeOnMainThreadAsync(() =>
            {
                HasActiveSwaps = activeSwaps.Any(); // todo: use swaps count to calculate reserved fee more accurately

            }).ConfigureAwait(false);
        }


        protected override void FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;
            selectFromViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            _navigationService?.ShowPage(new SelectAddressPage(selectFromViewModel), TabNavigation.Portfolio);
        }

        protected override void ToClick()
        {
            SelectToViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            _navigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount<TezosAccount>(_currency.Name);

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
                Log.Error(e, "{@currency}: update amount error", _currency?.Description);
            }
        }

        protected override async Task UpdateFee()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = _app.Account
                        .GetCurrencyAccount<TezosAccount>(_currency.Name);

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
                Log.Error(e, "{@currency}: update fee error", _currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount<TezosAccount>(_currency.Name);

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

                var (fa12TransferFee, _) = await _app.Account
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
                Log.Error(e, "{@currency}: max click error", _currency?.Description);
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

            var (fa12TransferFee, _) = await _app.Account
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
                        _currency.Name),
                    string.Format(
                        AppResources.MaxAmountToSendWithActiveSwapsDetails,
                        RecommendedMaxAmount,
                        _currency.Name));
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
                        _currency.Name),
                    string.Format(
                        AppResources.MaxAmountToSendWithActiveSwapsDetails,
                        RecommendedMaxAmount,
                        _currency.Name));
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
                        _currency.Name),
                    string.Format(
                        AppResources.MaxAmountToSendRecommendationDetails,
                        RecommendedMaxAmount,
                        _currency.Name));
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
            var account = _app.Account.GetCurrencyAccount<TezosAccount>(_currency.Name);

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