using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using Atomex;
using Atomex.Blockchain.Abstract;
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
                var account = _app.Account
                    .GetCurrencyAccount<Erc20Account>(_currency.Name);

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
                        GasPrice = decimal.ToInt32(_currency.GetFeePriceFromFeeAmount(maxAmountEstimation.Fee, GasLimit));
                    }
                    else
                    {
                        GasPrice = decimal.ToInt32(await _currency.GetDefaultFeePriceAsync());
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
                Log.Error(e, "{@Currency}: update amount error", _currency?.Description);
            }
        }

        protected override async Task UpdateGasPrice()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = _app.Account
                        .GetCurrencyAccount<Erc20Account>(_currency.Name);

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
                Log.Error(e, "{@Currency}: update gas price error", _currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount<Erc20Account>(_currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        gasLimit: UseDefaultFee ? null : GasLimit,
                        gasPrice: UseDefaultFee ? null : GasPrice,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    GasPrice = decimal.ToInt32(_currency.GetFeePriceFromFeeAmount(maxAmountEstimation.Fee, GasLimit));

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
                Log.Error(e, "{@Currency}: max click error", _currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var ethQuote = quotesProvider.GetQuote(_currency.FeeCurrencyName, BaseCurrencyCode);

            Device.InvokeOnMainThreadAsync(() =>
            {
                AmountInBase = Amount * (quote?.Bid ?? 0m);
                FeeInBase = Fee * (ethQuote?.Bid ?? 0m);
                TotalAmountInBase = AmountInBase + FeeInBase;
            });
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = _app.Account.GetCurrencyAccount<Erc20Account>(_currency.Name);

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