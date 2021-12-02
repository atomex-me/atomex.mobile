using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.ViewModel.CurrencyViewModels;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class ConversionConfirmationViewModel : BaseViewModel
    {
        public event EventHandler OnSuccess;

        private static readonly TimeSpan SwapTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan SwapCheckInterval = TimeSpan.FromSeconds(3);

        private IAtomexApp AtomexApp { get; }
        private INavigation Navigation { get; }
        public IFromSource From { get; }
        public string To { get; }
        public string RedeemAddress { get; }
        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetAmountInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string TargetCurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }

        public string FromFeeCurrencyCode { get; set; }
        public string TargetFeeCurrencyCode { get; set; }

        public decimal EstimatedPrice { get; set; }
        public decimal EstimatedOrderPrice { get; set; }
        public decimal EstimatedPaymentFee { get; set; }
        public decimal EstimatedPaymentFeeInBase { get; set; }
        public decimal EstimatedRedeemFee { get; set; }
        public decimal EstimatedRedeemFeeInBase { get; set; }
        public decimal EstimatedMakerNetworkFee { get; set; }
        public decimal EstimatedMakerNetworkFeeInBase { get; set; }
        public decimal EstimatedTotalNetworkFeeInBase { get; set; }

        public decimal RewardForRedeem { get; set; }
        public decimal RewardForRedeemInBase { get; set; }
        public bool HasRewardForRedeem { get; set; }

        private float _opacity = 1f;
        public float Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                    return;

                _isLoading = value;

                if (_isLoading)
                    Opacity = 0.3f;
                else
                    Opacity = 1f;

                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public ConversionConfirmationViewModel(IAtomexApp app, INavigation navigation)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(Navigation));
        }

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ??= new Command(Convert);

        private async void Convert()
        {
            try
            {
                IsLoading = true;

                var error = await ConvertAsync();

                if (error != null)
                {
                    if (error.Code == Errors.PriceHasChanged)
                    {
                        await Application.Current.MainPage.DisplayAlert(AppResources.PriceChanged, error.Description, AppResources.AcceptButton);
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(AppResources.Error, error.Description, AppResources.AcceptButton);
                    }

                    IsLoading = false;
                    return;
                }
                IsLoading = false;

                OnSuccess?.Invoke(this, EventArgs.Empty);
                
            }
            catch (Exception e)
            {
                IsLoading = false;
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, "An error has occurred while sending swap", AppResources.AcceptButton);
                Log.Error(e, "Swap error.");
            }
        }

        private async Task<Error> ConvertAsync()
        {
            try
            {
                var account = AtomexApp.Account;

                var fromWallets = await GetFromAddressesAsync();

                foreach (var fromWallet in fromWallets)
                    if (fromWallet.Currency != FromCurrencyViewModel.Currency.Name)
                        fromWallet.Currency = FromCurrencyViewModel.Currency.Name;

                // check balances
                var errors = await BalanceChecker.CheckBalancesAsync(AtomexApp.Account, fromWallets);

                if (errors.Any())
                    return new Error(Errors.SwapError, GetErrorsDescription(errors));

                if (Amount == 0)
                    return new Error(Errors.SwapError, AppResources.AmountLessThanZeroError);

                if (Amount > 0 && !fromWallets.Any())
                    return new Error(Errors.SwapError, AppResources.InsufficientFunds);

                var symbol = AtomexApp.SymbolsProvider
                    .GetSymbols(AtomexApp.Account.Network)
                    .SymbolByCurrencies(FromCurrencyViewModel.Currency, ToCurrencyViewModel.Currency);

                var baseCurrency = AtomexApp.Account.Currencies.GetByName(symbol.Base);
                var side = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
                var terminal = AtomexApp.Terminal;
                var price = EstimatedPrice;
                var orderPrice = EstimatedOrderPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, AppResources.NoLiquidityError);

                var qty = AmountHelper.AmountToQty(side, Amount, price, baseCurrency.DigitsMultiplier);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = AmountHelper.QtyToAmount(side, symbol.MinimumQty, price, FromCurrencyViewModel.Currency.DigitsMultiplier);
                    var message = string.Format(CultureInfo.InvariantCulture, AppResources.MinimumAllowedQtyWarning, minimumAmount, FromCurrencyViewModel.Currency.Name);

                    return new Error(Errors.SwapError, message);
                }

                var order = new Order
                {
                    Symbol = symbol.Name,
                    TimeStamp = DateTime.UtcNow,
                    Price = orderPrice,
                    Qty = qty,
                    Side = side,
                    Type = OrderType.FillOrKill,
                    FromWallets = fromWallets.ToList(),
                    MakerNetworkFee = EstimatedMakerNetworkFee
                };

                await order.CreateProofOfPossessionAsync(account);

                terminal.OrderSendAsync(order);

                // wait for swap confirmation
                var timeStamp = DateTime.UtcNow;

                while (DateTime.UtcNow < timeStamp + SwapTimeout)
                {
                    await Task.Delay(SwapCheckInterval);

                    var currentOrder = terminal.Account.GetOrderById(order.ClientOrderId);

                    if (currentOrder == null)
                        continue;

                    if (currentOrder.Status == OrderStatus.Pending)
                        continue;

                    if (currentOrder.Status == OrderStatus.PartiallyFilled || currentOrder.Status == OrderStatus.Filled)
                    {
                        var swap = (await terminal.Account
                            .GetSwapsAsync())
                            .FirstOrDefault(s => s.OrderId == currentOrder.Id);

                        if (swap == null)
                            continue;

                        return null;
                    }

                    if (currentOrder.Status == OrderStatus.Canceled)
                        return new Error(Errors.PriceHasChanged, AppResources.PriceChangedError);

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

        private async Task<IEnumerable<WalletAddress>> GetFromAddressesAsync()
        {
            if (From is FromAddress fromAddress)
            {
                var walletAddress = await AtomexApp.Account
                    .GetAddressAsync(FromCurrencyViewModel.Currency.Name, fromAddress.Address);

                return new WalletAddress[] { walletAddress };
            }
            else if (From is FromOutputs fromOutputs)
            {
                var config = (BitcoinBasedConfig)FromCurrencyViewModel.Currency;

                return await Task.WhenAll(fromOutputs.Outputs
                    .Select(o => o.DestinationAddress(config.Network))
                    .Distinct()
                    .Select(async a => await AtomexApp.Account.GetAddressAsync(FromCurrencyViewModel.Currency.Name, a)));

            }

            throw new NotSupportedException("Not supported type of From field");
        }

        private string GetErrorsDescription(IEnumerable<BalanceError> errors)
        {
            var descriptions = errors.Select(e => e.Type switch
            {
                BalanceErrorType.FailedToGet => $"Balance check for address {e.Address} failed",
                BalanceErrorType.LessThanExpected => $"Balance for address {e.Address} is {e.ActualBalance.ToString(CultureInfo.InvariantCulture)} and less than local {e.LocalBalance.ToString(CultureInfo.InvariantCulture)}",
                _ => $"Balance for address {e.Address} has changed and needs to be updated"
            });

            return string.Join(". ", descriptions) + ". Please update your balance and try again!";
        }

        private ICommand _totalFeeCommand;
        public ICommand TotalFeeCommand => _totalFeeCommand ??= new Command(async () => await OnTotalFeeTapped());

        private async Task OnTotalFeeTapped()
        {
            string message = string.Format(
                   CultureInfo.InvariantCulture,
                   AppResources.TotalNetworkFeeDetail,
                   AppResources.PaymentFeeLabel,
                   FormattableString.Invariant($"{EstimatedPaymentFee} {FromCurrencyViewModel.FeeCurrencyCode}"),
                   FormattableString.Invariant($"{EstimatedPaymentFeeInBase:(0.00$)}"),
                   HasRewardForRedeem ?
                       AppResources.RewardForRedeemLabel :
                       AppResources.RedeemFeeLabel,
                   HasRewardForRedeem ?
                       FormattableString.Invariant($"{RewardForRedeem} {ToCurrencyViewModel.FeeCurrencyCode}") :
                       FormattableString.Invariant($"{EstimatedRedeemFee} {ToCurrencyViewModel.FeeCurrencyCode}"),
                   HasRewardForRedeem ?
                       FormattableString.Invariant($"{RewardForRedeemInBase:(0.00$)}") :
                       FormattableString.Invariant($"{EstimatedRedeemFeeInBase:(0.00$)}"),
                   AppResources.MakerFeeLabel,
                   FormattableString.Invariant($"{EstimatedMakerNetworkFee} {FromCurrencyViewModel.CurrencyCode}"),
                   FormattableString.Invariant($"{EstimatedMakerNetworkFeeInBase:(0.00$)}"),
                   AppResources.TotalNetworkFeeLabel,
                   FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:0.00$}"));

            await Application.Current.MainPage.DisplayAlert(AppResources.NetworkFee, message, AppResources.AcceptButton);
        }
    }
}
