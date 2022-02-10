using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using atomex.Views.Send;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace atomex.ViewModel.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {

        public Fa12SendViewModel(
            IAtomexApp app,
            CurrencyViewModel currencyViewModel)
            : base(app, currencyViewModel)
        {
            SelectFromViewModel = new SelectAddressViewModel(App.Account, Currency, Navigation, true)
            {
                ConfirmAction = ConfirmFromAddress
            };

            SelectToViewModel = new SelectAddressViewModel(App.Account, Currency, Navigation)
            {
                ConfirmAction = ConfirmToAddress
            };

            this.WhenAnyValue(
                vm => vm.Amount,
                vm => vm.Fee,
                (amount, fee) => Currency.IsToken ? amount : amount + fee
            )
            .Select(totalAmount => totalAmount.ToString(CultureInfo.InvariantCulture))
            .ToPropertyEx(this, vm => vm.TotalAmountString);
        }

        protected override async Task FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;
            selectFromViewModel.AddressSettingType = SelectAddressViewModel.SettingType.Change;

            await Navigation.PushAsync(new FromAddressPage(selectFromViewModel));
        }

        protected override async Task ToClick()
        {
            SelectToViewModel.AddressSettingType = SelectAddressViewModel.SettingType.Change;

            await Navigation.PushAsync(new ToAddressPage(SelectToViewModel));
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name);

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
                        .GetCurrencyAccount<Fa12Account>(Currency.Name);

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
                Log.Error(e, "{@currency}: update fee error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = App.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name);

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
                        text: maxAmountEstimation.Error.Description);
                    Amount = 0;
                    AmountString = Amount.ToString();
                    this.RaisePropertyChanged(nameof(AmountString));

                    return;
                }

                Amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount
                    : 0;

                if (Fee < maxAmountEstimation.Fee)
                    ShowMessage(
                        messageType: MessageType.Error,
                        element: RelatedTo.Fee,
                        text: AppResources.LowFees);

                AmountString = Amount.ToString();
                this.RaisePropertyChanged(nameof(AmountString));
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
            TotalAmountInBase = AmountInBase + FeeInBase;
        }

        protected override async Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var tokenConfig = (Fa12Config)Currency;
            var tokenContract = tokenConfig.TokenContractAddress;
            const int tokenId = 0;
            const string tokenType = "FA12";

            var tokenAddress = await TezosTokensSendViewModel.GetTokenAddressAsync(
                account: App.Account,
                address: From,
                tokenContract: tokenContract,
                tokenId: tokenId,
                tokenType: tokenType);

            var currencyName = App.Account.Currencies
                .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == tokenContract)
                ?.Name ?? "FA12";

            var tokenAccount = App.Account.GetTezosTokenAccount<Fa12Account>(
                currency: currencyName,
                tokenContract: tokenContract,
                tokenId: tokenId);

            return await tokenAccount.SendAsync(
                from: tokenAddress.Address,
                to: To,
                amount: Amount,
                fee: Fee,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}