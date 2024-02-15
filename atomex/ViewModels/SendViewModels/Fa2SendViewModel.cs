using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.Views;
using Atomex;
using Atomex.Blockchain.Abstract;
using atomex.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using atomex.Models;
using Atomex.TezosTokens;
using atomex.ViewModels.Abstract;
using atomex.ViewModels.CurrencyViewModels;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.Message;

namespace atomex.ViewModels.SendViewModels
{
    public class Fa2SendViewModel : SendViewModel
    {
        public Fa2SendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel,
            INavigationService navigationService)
            : base(app, currencyViewModel, navigationService)
        {
            var tokenConfig = (Fa2Config)Currency;
            var tokenContract = tokenConfig.TokenContractAddress;

            SelectFromViewModel = new SelectAddressViewModel(
                account: App.Account,
                currency: tokenConfig,
                navigationService: NavigationService,
                mode: SelectAddressMode.SendFrom,
                tokenContract: tokenContract)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(
                account: App.Account,
                currency: tokenConfig,
                navigationService: NavigationService,
                tokenContract: tokenContract)
            {
                ConfirmAction = ConfirmToAddress
            };
        }

        protected override void FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;
            
            if (selectFromViewModel == null) return;
            selectFromViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            NavigationService?.ShowPage(new SelectAddressPage(selectFromViewModel), TabNavigation.Portfolio);
        }

        protected override void ToClick()
        {
            SelectToViewModel.SelectAddressFrom = SelectAddressFrom.Change;

            NavigationService?.ShowPage(new SelectAddressPage(SelectToViewModel), TabNavigation.Portfolio);
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<Fa2Account>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    SetFeeFromString(maxAmountEstimation.Fee.ToString(CultureInfo.CurrentCulture));

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
                Log.Error(e, "{@Currency}: update amount error", Currency?.Description);
            }
        }

        protected override async Task UpdateFee()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = App.Account
                        .GetCurrencyAccount<Fa2Account>(Currency.Name);

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
                Log.Error(e, "{@Currency}: update fee error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<Fa2Account>(Currency.Name);

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
                SetAmountFromString(amount.ToString(CultureInfo.CurrentCulture));

                if (Fee < maxAmountEstimation.Fee)
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Fee,
                        text: AppResources.LowFees);
            }
            catch (Exception e)
            {
                Log.Error(e, "{@Currency}: max click error", Currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            try
            {
                if (sender is not IQuotesProvider quotesProvider)
                    return;
                
                if (App.Account?.Network == Network.TestNet)
                    return;

                var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
                var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);
                
                if (quote == null || xtzQuote == null) return;

                Device.InvokeOnMainThreadAsync(() =>
                {
                    AmountInBase = Amount * (quote?.Bid ?? 0m);
                    FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
                    TotalAmountInBase = AmountInBase + FeeInBase;
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Update quotes error on {@Currency}", CurrencyCode);
            }
        }

        protected override async Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var tokenConfig = (Fa2Config)Currency;
            var tokenContract = tokenConfig.TokenContractAddress;
            const int tokenId = 0;
            const string tokenType = "FA2";

            var tokenAddress = await TezosTokensSendViewModel.GetTokenAddressAsync(
                account: App.Account,
                address: From,
                tokenContract: tokenContract,
                tokenId: tokenId,
                tokenType: tokenType);

            var currencyName = App.Account.Currencies
                .FirstOrDefault(c => c is Fa2Config fa2 && fa2.TokenContractAddress == tokenContract)
                ?.Name ?? "FA2";

            var tokenAccount = App.Account.GetTezosTokenAccount<Fa2Account>(
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
