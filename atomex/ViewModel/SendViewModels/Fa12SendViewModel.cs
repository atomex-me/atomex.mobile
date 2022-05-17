using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.Message;

namespace atomex.ViewModel.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {

        public Fa12SendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel,
            INavigationService navigationService)
            : base(app, currencyViewModel, navigationService)
        {
            SelectFromViewModel = new SelectAddressViewModel(
                account: _app.Account,
                currency: _currency,
                navigationService: _navigationService,
                mode: SelectAddressMode.SendFrom)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(_app.Account, _currency, _navigationService)
            {
                ConfirmAction = ConfirmToAddress
            };
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
                    .GetCurrencyAccount<Fa12Account>(_currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    SetFeeFromString(maxAmountEstimation.Fee.ToString());

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

                if (Fee < maxAmountEstimation.Fee)
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Fee,
                        text: AppResources.LowFees);
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
                        .GetCurrencyAccount<Fa12Account>(_currency.Name);

                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: From,
                            type: BlockchainTransactionType.Output,
                            reserve: false);

                    if (maxAmountEstimation.Error != null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Amount,
                            tooltipText: maxAmountEstimation.Error.Details,
                            text: maxAmountEstimation.Error.Description);
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

                    if (Fee < maxAmountEstimation.Fee)
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.Fee,
                            text: AppResources.LowFees);
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
                    .GetCurrencyAccount<Fa12Account>(_currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee;

                if (maxAmountEstimation.Error != null)
                {
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        tooltipText: maxAmountEstimation.Error.Details,
                        text: maxAmountEstimation.Error.Description);
                    SetAmountFromString("0");

                    return;
                }

                var amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount
                    : 0;
                SetAmountFromString(amount.ToString());

                if (Fee < maxAmountEstimation.Fee)
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Fee,
                        text: AppResources.LowFees);
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", _currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);

            Device.InvokeOnMainThreadAsync(() =>
            {
                AmountInBase = Amount * (quote?.Bid ?? 0m);
                FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
                TotalAmountInBase = AmountInBase + FeeInBase;
            });
        }

        protected override async Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var tokenConfig = (Fa12Config)_currency;
            var tokenContract = tokenConfig.TokenContractAddress;
            const int tokenId = 0;
            const string tokenType = "FA12";

            var tokenAddress = await TezosTokensSendViewModel.GetTokenAddressAsync(
                account: _app.Account,
                address: From,
                tokenContract: tokenContract,
                tokenId: tokenId,
                tokenType: tokenType);

            var currencyName = _app.Account.Currencies
                .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == tokenContract)
                ?.Name ?? "FA12";

            var tokenAccount = _app.Account.GetTezosTokenAccount<Fa12Account>(
                currency: currencyName,
                tokenContract: tokenContract,
                tokenId: tokenId);

            var (_, error) = await tokenAccount
                .SendAsync(
                    from: tokenAddress.Address,
                    to: To,
                    amount: AmountToSend,
                    fee: Fee,
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return error;
        }
    }
}