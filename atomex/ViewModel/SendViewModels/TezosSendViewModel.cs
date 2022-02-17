using System;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using Serilog;

namespace atomex.ViewModel.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        public TezosSendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
            SelectFromViewModel = new SelectAddressViewModel(App.Account, Currency, Navigation, SelectAddressMode.SendFrom)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(App.Account, Currency, Navigation)
            {
                ConfirmAction = ConfirmToAddress
            };
        }

        protected override async Task FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;
            selectFromViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            await Navigation.PushAsync(new FromAddressPage(selectFromViewModel));
        }

        protected override async Task ToClick()
        {
            SelectToViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            await Navigation.PushAsync(new ToAddressPage(SelectToViewModel));
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
                        reserve: UseDefaultFee);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee;

                if (maxAmountEstimation.Error != null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: maxAmountEstimation.Error.Description);

                    return;
                }

                var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

                if (Amount + Fee > availableAmount)
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: AppResources.InsufficientFunds);
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
                            reserve: UseDefaultFee);

                    if (maxAmountEstimation.Error != null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            text: maxAmountEstimation.Error.Description);

                        return;
                    }

                    var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

                    if (Amount + Fee > availableAmount)
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
                            element: RelatedTo.Fee,
                            text: AppResources.LowFees);
                    }
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
                        reserve: UseDefaultFee);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee;

                if (maxAmountEstimation.Error != null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: maxAmountEstimation.Error.Description);

                    Amount = 0;
                    AmountString = Amount.ToString();
                    this.RaisePropertyChanged(nameof(AmountString));

                    return;
                }

                if (UseDefaultFee)
                {
                    Amount = maxAmountEstimation.Amount > 0
                        ? maxAmountEstimation.Amount
                        : 0;
                }
                else
                {
                    var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

                    Amount = availableAmount - Fee > 0
                        ? availableAmount - Fee
                        : 0;

                    if (Fee < maxAmountEstimation.Fee)
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Fee,
                            text: AppResources.LowFees);
                }

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
            var account = App.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: Amount,
                fee: Fee,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}