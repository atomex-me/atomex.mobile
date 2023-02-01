using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using Atomex;
using Atomex.Blockchain.Abstract;
using atomex.Common;
using Atomex.Common;
using Atomex.MarketData.Abstract;
using atomex.ViewModels.CurrencyViewModels;
using Atomex.Wallet.Ethereum;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.Message;

namespace atomex.ViewModels.SendViewModels
{
    public class Erc20SendViewModel : EthereumSendViewModel
    {

        public Erc20SendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel,
            INavigationService navigationService)
            : base(app, currencyViewModel, navigationService)
        {
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<Erc20Account>(Currency.Name);

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
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: maxAmountEstimation.Error.Description,
                        tooltipText: maxAmountEstimation.Error.Details);
                    return;
                }

                if (Amount > maxAmountEstimation.Amount)
                    ShowMessage(
                       messageType: MessageType.Error,
                       element: RelatedTo.Amount,
                       text: AppResources.InsufficientFunds);
            }
            catch (Exception e)
            {
                Log.Error(e, "{@Currency}: update amount error", Currency?.Description);
            }
        }

        protected override async Task UpdateGasPrice()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = App.Account
                        .GetCurrencyAccount<Erc20Account>(Currency.Name);

                    // estimate max amount with new GasPrice
                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        gasLimit: GasLimit,
                        gasPrice: GasPrice,
                        reserve: false);

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
                        ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Amount,
                        text: AppResources.InsufficientFunds);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@Currency}: update gas price error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<Erc20Account>(Currency.Name);

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

                var amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount
                    : 0;
                SetAmountFromString(amount.ToString(CultureInfo.CurrentCulture));
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

                var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
                var ethQuote = quotesProvider.GetQuote(Currency.FeeCurrencyName, BaseCurrencyCode);
            
                if (quote == null || ethQuote == null) return;

                Device.InvokeOnMainThreadAsync(() =>
                {
                    AmountInBase = Amount * (quote?.Bid ?? 0m);
                    FeeInBase = Fee * (ethQuote?.Bid ?? 0m);
                    TotalAmountInBase = AmountInBase + FeeInBase;
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Update quotes error on {@Currency}", CurrencyCode);
            }
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = App.Account.GetCurrencyAccount<Erc20Account>(Currency.Name);

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