using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using Serilog;
using atomex.Common;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;
using Atomex.Client.Entities;
using atomex.ViewModels.CurrencyViewModels;
using Currencies = Atomex.Currencies;

namespace atomex.ViewModels.ConversionViewModels
{
    public class ConversionConfirmationViewModel : BaseViewModel
    {
        private static readonly TimeSpan SwapTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan SwapCheckInterval = TimeSpan.FromSeconds(3);
        public event EventHandler OnSuccess;

        private readonly IAtomexApp _app;
        private INavigationService _navigationService { get; set; }
        public IFromSource FromSource { get; set; }
        public string FromAddressDescription
        {
            get
            {
                if (FromSource is FromAddress fromAddress)
                    return fromAddress.Address.TruncateAddress();

                if (FromSource is FromOutputs fromOutputs)
                {
                    if (fromOutputs.Outputs.Count() > 1)
                        return $"{fromOutputs.Outputs.Count()} outputs";

                    var network = (FromCurrencyViewModel?.Currency as BitcoinBasedConfig)?.Network;

                    if (network != null)
                        return fromOutputs.Outputs
                            .First()
                            .DestinationAddress(network);
                }

                return null;
            }
        }

        public string ToAddress { get; set; }
        public string RedeemFromAddress { get; set; }

        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public string BaseCurrencyCode { get; set; }
        public string QuoteCurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetAmountInBase { get; set; }
        public decimal EstimatedPrice { get; set; }
        public decimal EstimatedOrderPrice { get; set; }
        public decimal EstimatedMakerNetworkFee { get; set; }
        public decimal EstimatedTotalNetworkFeeInBase { get; set; }

        public bool IsLoading { get; set; }

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= ReactiveCommand.Create(Send);

        private ICommand _undoConfirmStageCommand;
        public ICommand UndoConfirmStageCommand => _undoConfirmStageCommand ??= new Command(() => _navigationService?.CloseBottomSheet());

        public ConversionConfirmationViewModel(IAtomexApp app, INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(_app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
        }

        private async void Send()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            this.RaisePropertyChanged(nameof(IsLoading));

            try
            {
                var error = await ConvertAsync();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    if (error != null)
                    {
                        if (error.Code == Errors.PriceHasChanged)
                            _navigationService?.DisplaySnackBar(MessageType.Error, AppResources.PriceChangedError);
                        else
                            _navigationService?.DisplaySnackBar(MessageType.Error, error.Description);

                        return;
                    }
                });
                OnSuccess?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(MessageType.Error, "An error has occurred while sending swap");
                });
                Log.Error(e, "Swap error.");
            }
            finally
            {
                IsLoading = false;
                this.RaisePropertyChanged(nameof(IsLoading));
            }
        }

        private async Task<Error> ConvertAsync()
        {
            try
            {
                var account = _app.Account;

                var fromWallets = await GetFromAddressesAsync();

                foreach (var fromWallet in fromWallets)
                    if (fromWallet.Currency != FromCurrencyViewModel.Currency.Name)
                        fromWallet.Currency = FromCurrencyViewModel.Currency.Name;

                // check balances
                var errors = await BalanceChecker.CheckBalancesAsync(_app.Account, fromWallets);

                if (errors.Any())
                    return new Error(Errors.SwapError, GetErrorsDescription(errors));

                if (Amount == 0)
                    return new Error(Errors.SwapError, AppResources.AmountLessThanZeroError);

                if (Amount > 0 && !fromWallets.Any())
                    return new Error(Errors.SwapError, AppResources.InsufficientFunds);

                var symbol = _app.SymbolsProvider
                    .GetSymbols(_app.Account.Network)
                    .SymbolByCurrencies(FromCurrencyViewModel.Currency, ToCurrencyViewModel.Currency);

                var baseCurrency = _app.Account.Currencies.GetByName(symbol.Base);
                var side = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
                var atomexClient = _app.AtomexClient;
                var price = EstimatedPrice;
                var orderPrice = EstimatedOrderPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, AppResources.NoLiquidityError);

                var qty = AmountHelper.AmountToSellQty(side, Amount, price, baseCurrency.DigitsMultiplier);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = AmountHelper.QtyToSellAmount(
                        side: side,
                        qty: symbol.MinimumQty,
                        price: price,
                        digitsMultiplier: FromCurrencyViewModel.Currency.DigitsMultiplier);

                    var message = string.Format(
                        provider: CultureInfo.InvariantCulture,
                        format: AppResources.MinimumAllowedQtyWarning,
                        arg0: minimumAmount,
                        arg1: FromCurrencyViewModel.Currency.Name);

                    return new Error(Errors.SwapError, message);
                }

                var isToBitcoinBased = Atomex.Currencies.IsBitcoinBased(ToCurrencyViewModel.Currency.Name);

                var order = new Order
                {
                    ClientOrderId   = Guid.NewGuid().ToByteArray().ToHexString(0, 16),
                    Status          = OrderStatus.Pending,
                    Symbol          = symbol.Name,
                    TimeStamp       = DateTime.UtcNow,
                    Price           = orderPrice,
                    Qty             = qty,
                    LeaveQty        = qty,
                    Side            = side,
                    Type            = OrderType.FillOrKill,
                    FromWallets     = fromWallets.ToList(),
                    MakerNetworkFee = EstimatedMakerNetworkFee,

                    FromAddress = FromSource is FromAddress fromAddress ? fromAddress.Address : null,
                    FromOutputs = FromSource is FromOutputs fromOutputs ? fromOutputs.Outputs.ToList() : null,

                    // for Bitcoin based currencies ToAddress must be Atomex wallet's address!
                    ToAddress = isToBitcoinBased
                        ? RedeemFromAddress
                        : ToAddress,
                    RedeemFromAddress = isToBitcoinBased
                        ? ToAddress
                        : RedeemFromAddress
                };

                await order
                    .CreateProofOfPossessionAsync(account);

                await _app.Account
                    .UpsertOrderAsync(order);

                atomexClient.OrderSendAsync(new Atomex.Client.V1.Entities.Order
                {
                    ClientOrderId = order.ClientOrderId,
                    Symbol        = order.Symbol,
                    TimeStamp     = order.TimeStamp,
                    Price         = order.Price,
                    Qty           = order.Qty,
                    Side          = order.Side,
                    Type          = order.Type,
                    FromWallets   = order.FromWallets
                        .Select(w => new Atomex.Client.V1.Entities.WalletAddress
                        {
                            Address           = w.Address,
                            Currency          = w.Currency,
                            Nonce             = w.Nonce,
                            ProofOfPossession = w.ProofOfPossession,
                            PublicKey         = w.PublicKey,
                        })
                        .ToList(),
                    BaseCurrencyContract = GetSwapContract(order.Symbol.BaseCurrency()),
                    QuoteCurrencyContract = GetSwapContract(order.Symbol.QuoteCurrency())
                });

                // wait for swap confirmation
                var timeStamp = DateTime.UtcNow;

                while (DateTime.UtcNow < timeStamp + SwapTimeout)
                {
                    await Task.Delay(SwapCheckInterval);

                    var currentOrder = _app.Account.GetOrderById(order.ClientOrderId);

                    if (currentOrder == null)
                        continue;

                    if (currentOrder.Status == OrderStatus.Pending)
                        continue;

                    if (currentOrder.Status == OrderStatus.PartiallyFilled || currentOrder.Status == OrderStatus.Filled)
                    {
                        var swap = (await _app.Account
                            .GetSwapsAsync())
                            .FirstOrDefault(s => s.OrderId == currentOrder.Id);

                        if (swap == null)
                            continue;

                        return null;
                    }

                    if (currentOrder.Status == OrderStatus.Canceled)
                        return new Error(Errors.PriceHasChanged, AppResources.PriceChanged);

                    if (currentOrder.Status == OrderStatus.Rejected)
                        return new Error(Errors.OrderRejected, AppResources.OrderRejectedError);
                }

                return new Error(Errors.TimeoutReached, AppResources.TimeoutReachedError);
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");

                return new Error(Errors.SwapError, AppResources.ConversionError);
            }
        }

        private string GetSwapContract(string currency)
        {
            if (currency == "ETH" || Currencies.IsEthereumToken(currency))
                return _app.Account.Currencies.Get<EthereumConfig>(currency).SwapContractAddress;

            if (currency == "XTZ" || Currencies.IsTezosToken(currency))
                return _app.Account.Currencies.Get<TezosConfig>(currency).SwapContractAddress;

            return null;
        }

        private async Task<IEnumerable<WalletAddress>> GetFromAddressesAsync()
        {
            if (FromSource is FromAddress fromAddress)
            {
                var walletAddress = await _app.Account
                    .GetAddressAsync(FromCurrencyViewModel.Currency.Name, fromAddress.Address);

                return new WalletAddress[] { walletAddress };
            }
            else if (FromSource is FromOutputs fromOutputs)
            {
                var config = (BitcoinBasedConfig)FromCurrencyViewModel.Currency;

                return await Task.WhenAll(fromOutputs.Outputs
                    .Select(o => o.DestinationAddress(config.Network))
                    .Distinct()
                    .Select(async a => await _app.Account.GetAddressAsync(FromCurrencyViewModel.Currency.Name, a)));

            }

            throw new NotSupportedException("Not supported type of From field");
        }

        private static string GetErrorsDescription(IEnumerable<BalanceError> errors)
        {
            var descriptions = errors.Select(e => e.Type switch
            {
                BalanceErrorType.FailedToGet => $"Balance check for address {e.Address} failed",
                BalanceErrorType.LessThanExpected => $"Balance for address {e.Address} is " +
                    $"{e.ActualBalance.ToString(CultureInfo.InvariantCulture)} and less than" +
                    $" local {e.LocalBalance.ToString(CultureInfo.InvariantCulture)}",
                _ => $"Balance for address {e.Address} has changed and needs to be updated"
            });

            return string.Join(". ", descriptions) + ". Please update your balance and try again!";
        }
    }
}
