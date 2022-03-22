﻿using System.Collections.Generic;
using System.Linq;
using Atomex;
using Atomex.Core;
using System.Threading.Tasks;
using Atomex.Common;
using System;
using Atomex.MarketData;
using Serilog;
using Xamarin.Forms;
using Atomex.MarketData.Abstract;
using System.Globalization;
using System.Collections.ObjectModel;
using Atomex.Swaps;
using Atomex.Abstract;
using atomex.Resources;
using System.Windows.Input;
using atomex.Views.CreateSwap;
using Atomex.Services;
using atomex.ViewModel.CurrencyViewModels;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using System.Reactive.Linq;
using atomex.ViewModel.ConversionViewModels;
using atomex.ViewModel.SendViewModels;
using atomex.Common;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Wallet.BitcoinBased;
using Atomex.ViewModels;
using atomex.Models;
using atomex.Views.Send;
using Rg.Plugins.Popup.Services;
using atomex.Views;

namespace atomex.ViewModel
{
    public class ConversionViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        public INavigation Navigation { get; set; }

        private ISymbols Symbols
        {
            get
            {
                return _app.SymbolsProvider
                    .GetSymbols(_app.Account.Network);
            }
        }
        private ICurrencies Currencies
        {
            get
            {
                return _app.Account.Currencies;
            }
        }

        public class Grouping<K, T> : ObservableCollection<T>
        {
            public K Date { get; private set; }
            public Grouping(K date, IEnumerable<T> items)
            {
                Date = date;
                foreach (T item in items)
                    Items.Add(item);
            }
        }

        public string ToAddress => (ToCurrencyViewModelItem as SelectCurrencyWithAddressViewModelItem)?.SelectedAddress?.Address;
        [Reactive] public string RedeemFromAddress { get; set; }
        [Reactive] public bool UseRedeemAddress { get; set; }
        [Reactive] public List<CurrencyViewModel> FromCurrencies { get; set; }
        [Reactive] public List<CurrencyViewModel> ToCurrencies { get; set; }
        [Reactive] public ConversionCurrencyViewModel FromViewModel { get; set; }
        [Reactive] public ConversionCurrencyViewModel ToViewModel { get; set; }
        [Reactive] public SelectCurrencyViewModelItem FromCurrencyViewModelItem { get; set; }
        [Reactive] public SelectCurrencyViewModelItem ToCurrencyViewModelItem { get; set; }
        [Reactive] public string BaseCurrencyCode { get; set; }
        [Reactive] public string QuoteCurrencyCode { get; set; }

        public AmountType _amountType;

        private decimal _estimatedOrderPrice;

        [Reactive] public decimal EstimatedPrice { get; set; }
        [Reactive] public decimal EstimatedMaxFromAmount { get; set; }
        [Reactive] public decimal EstimatedMaxToAmount { get; set; }
        [Reactive] public decimal EstimatedMakerNetworkFee { get; set; }
        [Reactive] public decimal EstimatedMakerNetworkFeeInBase { get; set; }
        [Reactive] public decimal EstimatedPaymentFee { get; set; }
        [Reactive] public decimal EstimatedPaymentFeeInBase { get; set; }
        [Reactive] public decimal EstimatedRedeemFee { get; set; }
        [Reactive] public decimal EstimatedRedeemFeeInBase { get; set; }
        [Reactive] public decimal EstimatedTotalNetworkFeeInBase { get; set; }
        [Reactive] public decimal RewardForRedeem { get; set; }
        [Reactive] public decimal RewardForRedeemInBase { get; set; }
        [ObservableAsProperty] public bool HasRewardForRedeem { get; }
        [Reactive] public ObservableCollection<SwapViewModel> Swaps { get; set; }

        [Reactive] public Message Message { get; set; }
        [Reactive] public Message AmountToFeeRatioWarning { get; set; }
        [Reactive] public Message ExternalAddressWarning { get; set; }
        [Reactive] public Message RedeemFromAddressNote { get; set; }
        [Reactive] public bool CanExchange { get; set; }
        [Reactive] public bool IsNoLiquidity { get; set; }
        [Reactive] public bool IsInsufficientFunds { get; set; }
        [Reactive] public bool IsToAddressExtrenal { get; set; }
        [Reactive] public bool IsRedeemFromAddressWithMaxBalance { get; set; }

        private SwapViewModel _selectedSwap;
        public SwapViewModel SelectedSwap
        {
            get => _selectedSwap;
            set
            {
                if (value == null) return;
                _selectedSwap = value;

                _ = PopupNavigation.Instance.PushAsync(new SwapBottomSheet(_selectedSwap));
            }
        }

        [Reactive] public ObservableCollection<Grouping<DateTime, SwapViewModel>> GroupedSwaps { get; set; }
        private Dictionary<long, SwapViewModel> _cachedSwaps;
        [Reactive] public bool CanShowMoreSwaps { get; set; }
        [Reactive] public bool IsAllSwapsShowed { get; set; }
        private int _swapNumberPerPage = 3;

        public ConversionViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            _cachedSwaps = new Dictionary<long, SwapViewModel>();
            Swaps = new ObservableCollection<SwapViewModel>();
            GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>();
            Message = new Message();
            AmountToFeeRatioWarning = new Message();
            ExternalAddressWarning = new Message();
            RedeemFromAddressNote = new Message();
            CanExchange = true;

            FromViewModel = new ConversionCurrencyViewModel
            {
                MaxClicked = MaxClicked,
                SelectCurrencyClicked = async () =>
                {
                    var selectCurrencyViewModel = new SelectCurrencyViewModel(
                        account: _app.Account,
                        navigation: Navigation,
                        type: SelectCurrencyType.From,
                        currencies: await CreateFromCurrencyViewModelItemsAsync(FromCurrencies!),
                        selected: FromCurrencyViewModelItem)
                    {
                        CurrencySelected = i =>
                        {
                            FromCurrencyViewModelItem = i;

                            FromViewModel!.CurrencyViewModel = FromCurrencies!.First(c => c.Currency.Name == i.CurrencyViewModel.Currency.Name);
                            FromViewModel.Address = i.ShortAddressDescription;

                            Navigation.PopAsync();
                        }
                    };

                    await Navigation.PushAsync(new SelectCurrencyPage(selectCurrencyViewModel));
                },
                GotInputFocus = () => {
                    _amountType = AmountType.Sold;
                }
            };

            ToViewModel = new ConversionCurrencyViewModel
            {
                SelectCurrencyClicked = async () =>
                {
                    var selectCurrencyViewModel = new SelectCurrencyViewModel(
                        account: _app.Account,
                        navigation: Navigation,
                        type: SelectCurrencyType.To,
                        currencies: await CreateToCurrencyViewModelItemsAsync(ToCurrencies!),
                        selected: ToCurrencyViewModelItem)
                    {
                        CurrencySelected = i =>
                        {
                            ToCurrencyViewModelItem = i;

                            ToViewModel!.CurrencyViewModel = ToCurrencies!.First(c => c.Currency.Name == i.CurrencyViewModel.Currency.Name);
                            ToViewModel.Address = i.ShortAddressDescription;

                            Navigation.PopAsync();
                        }
                    };

                    await Navigation.PushAsync(new SelectCurrencyPage(selectCurrencyViewModel));
                },
                GotInputFocus = () => {
                    _amountType = AmountType.Purchased;
                }
            };

            GetSwaps();
            IsAllSwapsShowed = false;

            // FromCurrencyViewModel changed => Update ToCurrencies
            this.WhenAnyValue(vm => vm.FromViewModel.CurrencyViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(c =>
                {
                    ToCurrencies = FromCurrencies
                        ?.Where(fc => Symbols.SymbolByCurrencies(fc.Currency, c.Currency) != null)
                        .ToList();
                });

            // ToCurrencies list changed => check & update ToViewModel and ToCurrencyViewModelItem
            this.WhenAnyValue(vm => vm.ToCurrencies)
                .SubscribeInMainThread(c =>
                {
                    if (ToViewModel.CurrencyViewModel == null)
                        return;

                    var existsViewModel = ToCurrencies?.FirstOrDefault(
                        c => c.Currency.Name == ToViewModel.CurrencyViewModel.Currency.Name);

                    if (existsViewModel == null)
                    {
                        ToCurrencyViewModelItem = null;
                        ToViewModel.CurrencyViewModel = null;
                        ToViewModel.Address = null;
                        return;
                    }
                });

            this.WhenAnyValue(vm => vm.ToCurrencyViewModelItem)
                .SubscribeInMainThread(i =>
                {
                    if (i == null || i is not SelectCurrencyWithAddressViewModelItem item || item.SelectedAddress == null)
                    {
                        UseRedeemAddress = false;
                        RedeemFromAddress = null;
                        IsToAddressExtrenal = false;
                        IsRedeemFromAddressWithMaxBalance = false;
                        return;
                    }

                    var isBtcBased = Atomex.Currencies.IsBitcoinBased(i.CurrencyViewModel.Currency.Name);
                    UseRedeemAddress = !isBtcBased;

                    if (item.SelectedAddress.KeyIndex != null) // is atomex address
                    {
                        RedeemFromAddress = ToAddress;
                        IsToAddressExtrenal = false;
                        IsRedeemFromAddressWithMaxBalance = false;
                    }
                    else // is external address
                    {
                        if (isBtcBased)
                        {
                            RedeemFromAddress = _app.Account
                                .GetCurrencyAccount<BitcoinBasedAccount>(i.CurrencyViewModel.Currency.Name)
                                .GetFreeInternalAddressAsync()
                                .WaitForResult()
                                .Address;

                            IsRedeemFromAddressWithMaxBalance = false;
                        }
                        else
                        {
                            RedeemFromAddress = _app.Account
                                .GetUnspentAddressesAsync(i.CurrencyViewModel.Currency.FeeCurrencyName)
                                .WaitForResult()
                                .MaxByOrDefault(w => w.Balance)
                                ?.Address;

                            IsRedeemFromAddressWithMaxBalance = RedeemFromAddress != null;
                        }

                        IsToAddressExtrenal = true;
                    }
                });

            this.WhenAnyValue(vm => vm.IsToAddressExtrenal)
               .SubscribeInMainThread(t =>
               {
                   if (IsToAddressExtrenal)
                   {
                       ExternalAddressWarning.Type = MessageType.Warning;
                       ExternalAddressWarning.RelatedTo = RelatedTo.All;
                       ExternalAddressWarning.Text = string.Format(AppResources.AddressIsNotAtomex, ToAddress);
                       ExternalAddressWarning.TooltipText = AppResources.AddressIsNotAtomexToolTip;
                   }
                   else
                   {
                       ExternalAddressWarning.Text = string.Empty;
                       ExternalAddressWarning.TooltipText = string.Empty;
                   }

                   this.RaisePropertyChanged(nameof(ExternalAddressWarning));
               });

            this.WhenAnyValue(
                    vm => vm.IsRedeemFromAddressWithMaxBalance,
                    vm => vm.RedeemFromAddress)
                .SubscribeInMainThread(t =>
                {
                    if (IsRedeemFromAddressWithMaxBalance)
                    {
                        RedeemFromAddressNote.Type = MessageType.Warning;
                        RedeemFromAddressNote.RelatedTo = RelatedTo.All;
                        RedeemFromAddressNote.Text = string.Format(AppResources.RedeemFromAddressNote, RedeemFromAddress);
                        RedeemFromAddressNote.TooltipText = string.Format(AppResources.RedeemFromAddressNoteToolTip, RedeemFromAddress);
                    }
                    else
                    {
                        RedeemFromAddressNote.Text = string.Empty;
                        RedeemFromAddressNote.TooltipText = string.Empty;
                    }

                    this.RaisePropertyChanged(nameof(RedeemFromAddressNote));
                });

            // FromCurrencyViewModel or ToCurrencyViewModel changed
            this.WhenAnyValue(
                    vm => vm.FromViewModel.CurrencyViewModel,
                    vm => vm.ToViewModel.CurrencyViewModel)
                .WhereAllNotNull()
                .SubscribeInMainThread(t =>
                {
                    var symbol = Symbols.SymbolByCurrencies(t.Item1.Currency, t.Item2.Currency);

                    BaseCurrencyCode = symbol?.Base;
                    QuoteCurrencyCode = symbol?.Quote;
                });

            // Amount, FromCurrencyViewModel or ToCurrencyViewModel changed => estimate swap price and target amount
            this.WhenAnyValue(
                    vm => vm.FromViewModel.Amount,
                    vm => vm.FromViewModel.CurrencyViewModel,
                    vm => vm.FromViewModel.Address,
                    vm => vm.ToViewModel.Amount,
                    vm => vm.ToViewModel.CurrencyViewModel,
                    vm => vm.ToViewModel.Address,
                    vm => vm.RedeemFromAddress)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(a =>
                {
                    _ = EstimateSwapParamsAsync();
                    OnQuotesUpdatedEventHandler(sender: this, args: null);
                });

            // From Amount changed => update FromViewModel.AmountInBase
            this.WhenAnyValue(vm => vm.FromViewModel.Amount)
                .SubscribeInMainThread(amount => UpdateFromAmountInBase());

            // To Amount changed => update ToViewModel.AmountInBase
            this.WhenAnyValue(vm => vm.ToViewModel.Amount)
                .SubscribeInMainThread(amount => UpdateToAmountInBase());

            // EstimatedPaymentFee changed => update EstimatedPaymentFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedPaymentFee)
                .SubscribeInMainThread(amount => UpdateEstimatedPaymentFeeInBase());

            // EstimatedRedeemFee changed => update EstimatedRedeemFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedRedeemFee)
                .SubscribeInMainThread(amount => UpdateEstimatedRedeemFeeInBase());

            // RewardForRedeem changed => update RewardForRedeemInBase
            this.WhenAnyValue(vm => vm.RewardForRedeem)
                .SubscribeInMainThread(amount => UpdateRewardForRedeemInBase());

            // RewardForRedeem changed => update HasRewardForRedeem
            this.WhenAnyValue(vm => vm.RewardForRedeem)
                .Select(r => r > 0)
                .ToPropertyExInMainThread(this, vm => vm.HasRewardForRedeem);

            // EstimatedMakerNetworkFee changed => update EstimatedMakerNetworkFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedMakerNetworkFee)
                .SubscribeInMainThread(amount => UpdateEstimatedMakerNetworkFeeInBase());

            // If fees in base currency changed => update TotalNetworkFeeInBase
            this.WhenAnyValue(
                    vm => vm.EstimatedPaymentFeeInBase,
                    vm => vm.HasRewardForRedeem,
                    vm => vm.EstimatedRedeemFeeInBase,
                    vm => vm.EstimatedMakerNetworkFeeInBase,
                    vm => vm.RewardForRedeemInBase)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(t => UpdateTotalNetworkFeeInBase());
            
            // AmountInBase or EstimatedTotalNetworkFeeInBase changed => check the ratio of the fee to the amount
            this.WhenAnyValue(
                    vm => vm.FromViewModel.AmountInBase,
                    vm => vm.EstimatedTotalNetworkFeeInBase)
                .SubscribeInMainThread(t => CheckAmountToFeeRatio());

            this.WhenAnyValue(
                    vm => vm.IsInsufficientFunds,
                    vm => vm.IsNoLiquidity)
                .SubscribeInMainThread(t =>
                {
                    FromViewModel.IsAmountValid = !IsInsufficientFunds && !IsNoLiquidity;
                    ToViewModel.IsAmountValid = !IsInsufficientFunds && !IsNoLiquidity;

                    if (IsInsufficientFunds)
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.All,
                            text: AppResources.InsufficientFunds);
                    if (IsNoLiquidity)
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.All,
                            text: AppResources.NoLiquidityError);
                });

            this.WhenAnyValue(
                    vm => vm.IsInsufficientFunds,
                    vm => vm.IsNoLiquidity,
                    vm => vm.FromViewModel.CurrencyViewModel,
                    vm => vm.FromViewModel.IsAmountValid,
                    vm => vm.ToViewModel.CurrencyViewModel,
                    vm => vm.ToViewModel.IsAmountValid,
                    vm => vm.FromViewModel.AmountInBase)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(t =>
                {
                    var estimatedTotalNetworkFeeInBase = EstimatedTotalNetworkFeeInBase;
                    var amountInBase = FromViewModel.AmountInBase;
                    var isGoodAmountToFeeRatio = amountInBase == 0 || estimatedTotalNetworkFeeInBase / amountInBase <= 0.75m;

                    CanExchange = !IsInsufficientFunds &&
                        !IsNoLiquidity &&
                        FromViewModel?.CurrencyViewModel != null &&
                        FromViewModel?.IsAmountValid == true &&
                        FromCurrencyViewModelItem != null &&
                        ToViewModel?.CurrencyViewModel != null &&
                        ToViewModel?.IsAmountValid == true &&
                        ToCurrencyViewModelItem != null &&
                        EstimatedPrice > 0 &&
                        isGoodAmountToFeeRatio;
                });

            this.WhenAnyValue(vm => vm.Swaps)
                .Subscribe(swaps => CanShowMoreSwaps = swaps.Count > _swapNumberPerPage);

            this.WhenAnyValue(vm => vm.IsAllSwapsShowed)
                .Subscribe(flag =>
                {
                    var groups = !flag
                        ? Swaps
                           .OrderByDescending(p => p.LocalTime.Date)
                           .Take(_swapNumberPerPage)
                           .GroupBy(p => p.LocalTime.Date)
                           .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))))
                        : Swaps
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))));

                    GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>(groups);
                    this.RaisePropertyChanged(nameof(GroupedSwaps));
                });

            SubscribeToServices();
        }

        public async void MaxClicked()
        {
            try
            {
                _amountType = AmountType.Sold;

                // error to currency is empty
                if (FromViewModel.CurrencyViewModel == null || ToViewModel.CurrencyViewModel == null)
                    return;

                var swapParams = await Atomex.ViewModels.Helpers
                    .EstimateSwapParamsAsync(
                        from: FromCurrencyViewModelItem?.FromSource,
                        fromAmount: EstimatedMaxFromAmount,
                        redeemFromAddress: RedeemFromAddress,
                        fromCurrency: FromViewModel.CurrencyViewModel?.Currency,
                        toCurrency: ToViewModel.CurrencyViewModel?.Currency,
                        account: _app.Account,
                        atomexClient: _app.Terminal,
                        symbolsProvider: _app.SymbolsProvider,
                        quotesProvider: _app.QuotesProvider);

                if (swapParams == null)
                    return;

                FromViewModel.SetAmountFromString(Math.Min(swapParams.Amount, EstimatedMaxFromAmount).ToString());
            }
            catch (Exception e)
            {
                Log.Error(e, "Max amount error.");
            }
        }

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ??= ReactiveCommand.Create(OnConvertClick);

        private ICommand _swapCurrenciesCommand;
        public ICommand SwapCurrenciesCommand => _swapCurrenciesCommand ??= ReactiveCommand.Create(async () =>
        {
            if (FromViewModel.CurrencyViewModel == null || ToViewModel.CurrencyViewModel == null)
                return;

            var previousFromCurrency = FromViewModel.CurrencyViewModel;

            FromViewModel.CurrencyViewModel = ToViewModel.CurrencyViewModel;
            FromCurrencyViewModelItem = await CreateFromCurrencyViewModelItemAsync(FromViewModel.CurrencyViewModel);
            FromViewModel.Address = FromCurrencyViewModelItem.ShortAddressDescription;

            ToViewModel.CurrencyViewModel = previousFromCurrency;
            ToCurrencyViewModelItem = await CreateToCurrencyViewModelItemAsync(ToViewModel.CurrencyViewModel);
            ToViewModel.Address = ToCurrencyViewModelItem.ShortAddressDescription;
        });

        private ICommand _changeRedeemAddress;
        public ICommand ChangeRedeemAddress => _changeRedeemAddress ??= ReactiveCommand.Create(async () =>
        {
            if (ToViewModel.CurrencyViewModel == null)
                return;

            var item = await CreateToCurrencyViewModelItemAsync(ToViewModel.CurrencyViewModel);

            var feeCurrencyName = ToViewModel
                .CurrencyViewModel
                .Currency
                .FeeCurrencyName;

            var feeCurrency = FromCurrencies
                !.First(c => c.Currency.Name == feeCurrencyName)
                .Currency;
            
            var selectAddressViewModel = new SelectAddressViewModel(
                account: _app.Account,
                currency: feeCurrency,
                navigation: Navigation,
                mode: SelectAddressMode.ChangeRedeemAddress,
                selectedAddress: RedeemFromAddress)
            {
                ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                {
                    RedeemFromAddress = walletAddressViewModel.Address;
                    Navigation.PopAsync();
                }
            };

            await Navigation.PushAsync(new SelectAddressPage(selectAddressViewModel));
        });

        private async Task<SelectCurrencyViewModelItem> CreateFromCurrencyViewModelItemAsync(
           CurrencyViewModel currencyViewModel)
        {
            var currencyName = currencyViewModel.Currency.Name;

            if (Atomex.Currencies.IsBitcoinBased(currencyName))
            {
                var availableOutputs = (await _app.Account
                    .GetCurrencyAccount<BitcoinBasedAccount>(currencyName)
                    .GetAvailableOutputsAsync()
                    .ConfigureAwait(false))
                    .Cast<BitcoinBasedTxOutput>();

                var selectedOutputs = FromCurrencyViewModelItem?.CurrencyViewModel.Currency.Name == currencyName
                    ? (FromCurrencyViewModelItem as SelectCurrencyWithOutputsViewModelItem)?.SelectedOutputs
                    : null;

                return new SelectCurrencyWithOutputsViewModelItem(
                    currencyViewModel: currencyViewModel,
                    availableOutputs: availableOutputs,
                    selectedOutputs: selectedOutputs);
            }
            else
            {
                var availableAddresses = await _app.Account
                    .GetUnspentAddressesAsync(currencyName)
                    .ConfigureAwait(false);

                if (!availableAddresses.Any())
                {
                    availableAddresses = new List<WalletAddress>()
                    {
                        await _app.Account
                            .GetFreeExternalAddressAsync(currencyName)
                            .ConfigureAwait(false)
                    };
                }

                var selectedAddress = FromCurrencyViewModelItem?.CurrencyViewModel.Currency.Name == currencyName
                    ? (FromCurrencyViewModelItem as SelectCurrencyWithAddressViewModelItem)?.SelectedAddress
                    : availableAddresses.MaxByOrDefault(w => w.AvailableBalance());

                return new SelectCurrencyWithAddressViewModelItem(
                    currencyViewModel: currencyViewModel,
                    type: SelectCurrencyType.From,
                    availableAddresses: availableAddresses,
                    selectedAddress: selectedAddress);
            }
        }

        private async Task<IEnumerable<SelectCurrencyViewModelItem>> CreateFromCurrencyViewModelItemsAsync(
            IEnumerable<CurrencyViewModel> currencyViewModels)
        {
            var result = new List<SelectCurrencyViewModelItem>();

            foreach (var currencyViewModel in currencyViewModels)
            {
                var currencyViewModelItem = await CreateFromCurrencyViewModelItemAsync(currencyViewModel)
                    .ConfigureAwait(false);

                result.Add(currencyViewModelItem);
            }

            return result;
        }

        private async Task<SelectCurrencyViewModelItem> CreateToCurrencyViewModelItemAsync(
            CurrencyViewModel currencyViewModel)
        {
            var currencyName = currencyViewModel.Currency.Name;

            var receivingAddresses = await AddressesHelper
                .GetReceivingAddressesAsync(
                    account: _app.Account,
                    currency: currencyViewModel.Currency)
                .ConfigureAwait(false);

            var selectedAddress = ToCurrencyViewModelItem?.CurrencyViewModel.Currency.Name == currencyName
                ? (ToCurrencyViewModelItem as SelectCurrencyWithAddressViewModelItem)?.SelectedAddress
                : Atomex.Currencies.IsBitcoinBased(currencyName)
                    ? receivingAddresses.FirstOrDefault(w => w.IsFreeAddress)?.WalletAddress
                    : receivingAddresses.MaxByOrDefault(w => w.AvailableBalance)?.WalletAddress;

            return new SelectCurrencyWithAddressViewModelItem(
                currencyViewModel: currencyViewModel,
                type: SelectCurrencyType.To,
                availableAddresses: receivingAddresses.Select(a => a.WalletAddress),
                selectedAddress: selectedAddress);
        }

        private async Task<IEnumerable<SelectCurrencyViewModelItem>> CreateToCurrencyViewModelItemsAsync(
            IEnumerable<CurrencyViewModel> currencyViewModels)
        {
            var result = new List<SelectCurrencyViewModelItem>();

            foreach (var currencyViewModel in currencyViewModels)
            {
                var currencyViewModelItem = await CreateToCurrencyViewModelItemAsync(currencyViewModel)
                    .ConfigureAwait(false);

                result.Add(currencyViewModelItem);
            }

            return result;
        }

        public async void SetFromCurrency(CurrencyConfig fromCurrency)
        {
            FromViewModel.CurrencyViewModel = FromCurrencies?.FirstOrDefault(vm => vm.Currency.Name == fromCurrency.Name);

            if (FromViewModel.CurrencyViewModel != null)
            {
                FromCurrencyViewModelItem = await CreateFromCurrencyViewModelItemAsync(FromViewModel.CurrencyViewModel);
                FromViewModel.Address = FromCurrencyViewModelItem.ShortAddressDescription;
            }
        }

        private void SubscribeToServices()
        {
            _app.AtomexClientChanged += OnTerminalChangedEventHandler;

            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;

            OnTerminalChangedEventHandler(this, new AtomexClientChangedEventArgs(_app.Terminal));
        }

        protected virtual async Task EstimateSwapParamsAsync()
        {
            try
            {
                // estimate max payment amount and max fee
                var swapParams = await Atomex.ViewModels.Helpers
                    .EstimateSwapParamsAsync(
                        from: FromCurrencyViewModelItem?.FromSource,
                        fromAmount: FromViewModel.Amount,
                        redeemFromAddress: RedeemFromAddress,
                        fromCurrency: FromViewModel.CurrencyViewModel?.Currency,
                        toCurrency: ToViewModel.CurrencyViewModel?.Currency,
                        account: _app.Account,
                        atomexClient: _app.Terminal,
                        symbolsProvider: _app.SymbolsProvider,
                        quotesProvider: _app.QuotesProvider);

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    if (swapParams == null)
                    {
                        EstimatedPaymentFee = 0;
                        EstimatedRedeemFee = 0;
                        RewardForRedeem = 0;
                        EstimatedMakerNetworkFee = 0;
                        
                        return;
                    }

                    if (swapParams.Error != null)
                    {
                        ShowMessage(
                            messageType: MessageType.Error,
                            element: RelatedTo.All,
                            text: swapParams.Error.Description,
                            tooltipText: swapParams.Error.Details);
                    }
                    else
                    {
                        Message.Text = string.Empty;
                        this.RaisePropertyChanged(nameof(Message));
                    }

                    EstimatedPaymentFee = swapParams.PaymentFee;
                    EstimatedRedeemFee = swapParams.RedeemFee;
                    RewardForRedeem = swapParams.RewardForRedeem;
                    EstimatedMakerNetworkFee = swapParams.MakerNetworkFee;
                    IsInsufficientFunds = swapParams.Error?.Code == Errors.InsufficientFunds;

                });
            }
            catch (Exception e)
            {
                Log.Error(e, "EstimateSwapParamsAsync error.");
            }
        }

        private static decimal TryGetAmountInBase(
            decimal amount,
            string currency,
            string baseCurrency,
            ICurrencyQuotesProvider provider,
            decimal defaultAmountInBase = 0)
        {
            if (currency == null || baseCurrency == null || provider == null)
                return defaultAmountInBase;

            var quote = provider.GetQuote(currency, baseCurrency);
            return amount * (quote?.Bid ?? 0m);
        }

        private void UpdateFromAmountInBase() => FromViewModel.AmountInBase = TryGetAmountInBase(
            amount: FromViewModel.Amount,
            currency: FromViewModel.CurrencyViewModel?.CurrencyCode,
            baseCurrency: FromViewModel.CurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: FromViewModel.AmountInBase);

        private void UpdateToAmountInBase() => ToViewModel.AmountInBase = TryGetAmountInBase(
            amount: ToViewModel.Amount,
            currency: ToViewModel.CurrencyViewModel?.CurrencyCode,
            baseCurrency: FromViewModel.CurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: ToViewModel.AmountInBase);

        private void UpdateEstimatedPaymentFeeInBase() => EstimatedPaymentFeeInBase = TryGetAmountInBase(
            amount: EstimatedPaymentFee,
            currency: FromViewModel.CurrencyViewModel?.Currency?.FeeCurrencyName,
            baseCurrency: FromViewModel.CurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedPaymentFeeInBase);

        private void UpdateEstimatedRedeemFeeInBase() => EstimatedRedeemFeeInBase = TryGetAmountInBase(
            amount: EstimatedRedeemFee,
            currency: ToViewModel.CurrencyViewModel?.Currency?.FeeCurrencyName,
            baseCurrency: FromViewModel.CurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: 0); // EstimatedRedeemFeeInBase);

        private void UpdateRewardForRedeemInBase() => RewardForRedeemInBase = TryGetAmountInBase(
            amount: RewardForRedeem,
            currency: ToViewModel.CurrencyViewModel?.CurrencyCode,
            baseCurrency: FromViewModel.CurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: RewardForRedeemInBase);

        private void UpdateEstimatedMakerNetworkFeeInBase() => EstimatedMakerNetworkFeeInBase = TryGetAmountInBase(
            amount: EstimatedMakerNetworkFee,
            currency: FromViewModel.CurrencyViewModel?.CurrencyCode,
            baseCurrency: FromViewModel.CurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedMakerNetworkFeeInBase);

        private void UpdateTotalNetworkFeeInBase() =>
            EstimatedTotalNetworkFeeInBase = EstimatedPaymentFeeInBase +
                (!HasRewardForRedeem ? EstimatedRedeemFeeInBase : 0) +
                EstimatedMakerNetworkFeeInBase +
                (HasRewardForRedeem ? RewardForRedeemInBase : 0);

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var terminal = args.AtomexClient;

            if (terminal?.Account == null) return;
            

            terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            terminal.SwapUpdated += OnSwapEventHandler;

            FromCurrencies = terminal.Account.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(c => CurrencyViewModelCreator.CreateViewModel(
                    app: _app,
                    currency: c,
                    loadTransactions: false))
                .ToList();

            ToCurrencies = FromCurrencies;

            FromViewModel.CurrencyViewModel = null;
            ToViewModel.CurrencyViewModel = null;

            FromCurrencyViewModelItem = null;
            ToCurrencyViewModelItem = null;

            OnSwapEventHandler(this, args: null);
        }

        private void CheckAmountToFeeRatio()
        {
            var estimatedTotalNetworkFeeInBase = EstimatedTotalNetworkFeeInBase;
            var amountInBase = FromViewModel.AmountInBase;

            if (amountInBase != 0 && estimatedTotalNetworkFeeInBase / amountInBase > 0.3m)
            {
                var message = string.Format(
                    provider: CultureInfo.CurrentCulture,
                    format: AppResources.TooHighNetworkFee,
                    arg0: FormattableString.Invariant($"{estimatedTotalNetworkFeeInBase:$0.00}"),
                    arg1: FormattableString.Invariant($"{estimatedTotalNetworkFeeInBase / amountInBase:0.00%}"));

                SetAmountToFeeRationWarning(
                    messageType: MessageType.Error,
                    element: RelatedTo.All,
                    text: message,
                    tooltipText: AppResources.AmountToFeeRatioToolTip);

                return;
            }
            if (amountInBase != 0 && estimatedTotalNetworkFeeInBase / amountInBase > 0.1m)
            {
                var message = string.Format(
                    provider: CultureInfo.CurrentCulture,
                    format: AppResources.SufficientNetworkFee,
                    arg0: FormattableString.Invariant($"{estimatedTotalNetworkFeeInBase:$0.00}"),
                    arg1: FormattableString.Invariant($"{estimatedTotalNetworkFeeInBase / amountInBase:0.00%}"));

                SetAmountToFeeRationWarning(
                    messageType: MessageType.Warning,
                    element: RelatedTo.All,
                    text: message,
                    tooltipText: AppResources.AmountToFeeRatioToolTip);

                return;
            }

            AmountToFeeRatioWarning.Text = string.Empty;
            this.RaisePropertyChanged(nameof(AmountToFeeRatioWarning));
        }

        protected async void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            await Device.InvokeOnMainThreadAsync(() =>
            {
                UpdateFromAmountInBase();
                UpdateEstimatedPaymentFeeInBase();
                UpdateEstimatedRedeemFeeInBase();
                UpdateRewardForRedeemInBase();
                UpdateEstimatedMakerNetworkFeeInBase();
                UpdateTotalNetworkFeeInBase();

            });
        }

        protected async void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            try
            {
                var swapPriceEstimation = await Atomex.ViewModels.Helpers
                    .EstimateSwapPriceAsync(
                        amount: _amountType == AmountType.Sold
                            ? FromViewModel.Amount
                            : ToViewModel.Amount,
                        amountType: _amountType,
                        fromCurrency: FromViewModel.CurrencyViewModel?.Currency,
                        toCurrency: ToViewModel.CurrencyViewModel?.Currency,
                        account: _app.Account,
                        atomexClient: _app.Terminal,
                        symbolsProvider: _app.SymbolsProvider);

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    if (swapPriceEstimation == null)
                    {
                        if (_amountType == AmountType.Sold)
                        {
                            ToViewModel.SetAmountFromString("0");
                        }
                        else
                        {
                            FromViewModel.SetAmountFromString("0");
                        }

                        EstimatedPrice = 0;
                        EstimatedMaxFromAmount = 0;
                        EstimatedMaxToAmount = 0;
                        IsNoLiquidity = false;
                        return;
                    }

                    _estimatedOrderPrice = swapPriceEstimation.OrderPrice;

                    if (_amountType == AmountType.Sold)
                    {
                        ToViewModel.SetAmountFromString(swapPriceEstimation.ToAmount.ToString());
                    }
                    else
                    {
                        FromViewModel.SetAmountFromString(swapPriceEstimation.FromAmount.ToString());
                    }

                    EstimatedPrice = swapPriceEstimation.Price;
                    EstimatedMaxFromAmount = swapPriceEstimation.MaxFromAmount;
                    EstimatedMaxToAmount = swapPriceEstimation.MaxToAmount;
                    IsNoLiquidity = swapPriceEstimation.IsNoLiquidity;
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Quotes updated event handler error");
            }
        }

        private ICommand _estNetworkFeeTooltipCommand;
        public ICommand EstNetworkFeeTooltipCommand => _estNetworkFeeTooltipCommand ??=
            ReactiveCommand.Create(async () =>
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    AppResources.TotalNetworkFeeDetail,
                    AppResources.PaymentFeeLabel,
                    FormattableString.Invariant($"{EstimatedPaymentFee} {FromViewModel?.CurrencyViewModel?.FeeCurrencyCode}"),
                    FormattableString.Invariant($"{EstimatedPaymentFeeInBase:(0.00$)}"),
                    HasRewardForRedeem
                        ? AppResources.RewardForRedeemLabel
                        : AppResources.RedeemFeeLabel,
                    HasRewardForRedeem
                        ? FormattableString.Invariant($"{RewardForRedeem} {ToViewModel?.CurrencyViewModel?.FeeCurrencyCode}")
                        : FormattableString.Invariant($"{EstimatedRedeemFee} {ToViewModel?.CurrencyViewModel?.FeeCurrencyCode}"),
                    HasRewardForRedeem
                        ? FormattableString.Invariant($"{RewardForRedeemInBase:(0.00$)}")
                        : FormattableString.Invariant($"{EstimatedRedeemFeeInBase:(0.00$)}"),
                    AppResources.MakerFeeLabel,
                    FormattableString.Invariant($"{EstimatedMakerNetworkFee} {FromViewModel?.CurrencyViewModel?.FeeCurrencyCode}"),
                    FormattableString.Invariant($"{EstimatedMakerNetworkFeeInBase:(0.00$)}"),
                    AppResources.TotalNetworkFeeLabel,
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:0.00$}"));

                await Application.Current.MainPage.DisplayAlert(AppResources.NetworkFee, message, AppResources.AcceptButton);
            });

        private ICommand _availableAmountTooltipCommand;
        public ICommand AvailableAmountTooltipCommand => _availableAmountTooltipCommand ??=
            ReactiveCommand.Create(async () => await Application.Current.MainPage.DisplayAlert(string.Empty, AppResources.AvailableAmountDexTooltip, AppResources.AcceptButton));

        private ICommand _showTooltipCommand;
        public ICommand ShowTooltipCommand => _showTooltipCommand ??=
            ReactiveCommand.Create<string>(async (tooltipText) =>
            {
                if (!string.IsNullOrEmpty(tooltipText))
                    await Application.Current.MainPage.DisplayAlert(string.Empty, tooltipText, AppResources.AcceptButton);
            });

        private ICommand _showAllSwapsCommand;
        public ICommand ShowAllSwapsCommand => _showAllSwapsCommand ??=
            ReactiveCommand.Create(() =>
            {
                IsAllSwapsShowed = true;
                CanShowMoreSwaps = false;
            });

        private async void OnSwapEventHandler(object sender, SwapEventArgs args)
        {
            try
            {
                if (args == null)
                    return;

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    if (_cachedSwaps.TryGetValue(args.Swap.Id, out SwapViewModel swap))
                    {
                        swap.UpdateSwap(args.Swap);
                    }
                    else
                    {
                        var swapViewModel = SwapViewModelFactory.CreateSwapViewModel(args.Swap, Currencies);
                        _cachedSwaps.Add(args.Swap.Id, swapViewModel);
                        Swaps.Add(swapViewModel);
                        this.RaisePropertyChanged(nameof(Swaps));

                        var groups = !IsAllSwapsShowed
                            ? Swaps
                               .OrderByDescending(p => p.LocalTime.Date)
                               .Take(_swapNumberPerPage)
                               .GroupBy(p => p.LocalTime.Date)
                               .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))))
                            : Swaps
                                .GroupBy(p => p.LocalTime.Date)
                                .OrderByDescending(g => g.Key)
                                .Select(g => new Grouping<DateTime, SwapViewModel>(g.Key, new ObservableCollection<SwapViewModel>(g.OrderByDescending(g => g.LocalTime))));

                        GroupedSwaps = new ObservableCollection<Grouping<DateTime, SwapViewModel>>(groups);
                        this.RaisePropertyChanged(nameof(GroupedSwaps));

                        if (PopupNavigation.Instance.PopupStack.Count > 0)
                            _ = PopupNavigation.Instance.PopAsync();
                        _ = PopupNavigation.Instance.PushAsync(new SwapBottomSheet(swapViewModel));
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Swaps update error");
            }
        }

        private async void GetSwaps()
        {
            try
            {
                var swaps = await _app.Account
                    .GetSwapsAsync();

                if (swaps == null)
                    return;

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _cachedSwaps.Clear();
                    var swps = new ObservableCollection<SwapViewModel>();
                    foreach (var swap in swaps)
                    {
                        var swapViewModel = SwapViewModelFactory.CreateSwapViewModel(swap, Currencies);

                        long.TryParse(swapViewModel.Id, out long id);
                        _cachedSwaps.Add(id, swapViewModel);
                        swps.Add(swapViewModel);
                    }
                
                    Swaps = new ObservableCollection<SwapViewModel>(swps);
                    this.RaisePropertyChanged(nameof(Swaps));
                });
            }
            catch(Exception e)
            {
                Log.Error(e, "Get swaps error");
            }
        }

        private async Task OnConvertClick()
        {
            if (FromViewModel.CurrencyViewModel == null ||
                ToViewModel.CurrencyViewModel == null ||
                FromCurrencyViewModelItem?.FromSource == null ||
                ToAddress == null)
                return;

            if (FromViewModel.Amount <= 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Warning,
                    AppResources.AmountLessThanZeroError,
                    AppResources.AcceptButton);
                return;
            }

            if (!FromViewModel.IsAmountValid || !ToViewModel.IsAmountValid)
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Warning,
                    AppResources.BigAmount,
                    AppResources.AcceptButton);
                return;
            }

            if (EstimatedPrice <= 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Warning,
                    AppResources.NoLiquidityError,
                    AppResources.AcceptButton);
                return;
            }

            if (!_app.Terminal.IsServiceConnected(TerminalService.All))
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error,
                    AppResources.ServicesUnavailable,
                    AppResources.AcceptButton);
                return;
            }

            var symbol = Symbols.SymbolByCurrencies(
                from: FromViewModel.CurrencyViewModel.Currency,
                to: ToViewModel.CurrencyViewModel.Currency);

            if (symbol == null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error,
                    AppResources.NotSupportedSymbol,
                    AppResources.AcceptButton);
                return;
            }

            var side = symbol.OrderSideForBuyCurrency(ToViewModel.CurrencyViewModel.Currency);
            var price = EstimatedPrice;
            var baseCurrency = Currencies.GetByName(symbol.Base);

            var qty = AmountHelper.AmountToSellQty(
                side: side,
                amount: FromViewModel.Amount,
                price: price,
                digitsMultiplier: baseCurrency.DigitsMultiplier);

            if (qty < symbol.MinimumQty)
            {
                var minimumAmount = AmountHelper.QtyToSellAmount(
                    side: side,
                    qty: symbol.MinimumQty,
                    price: price,
                    digitsMultiplier: FromViewModel.CurrencyViewModel.Currency.DigitsMultiplier);

                var message = string.Format(
                    provider: CultureInfo.CurrentCulture,
                    format: AppResources.MinimumAllowedQtyWarning,
                    arg0: minimumAmount,
                    arg1: FromViewModel.CurrencyViewModel.Currency.Name);

                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Warning,
                    message,
                    AppResources.AcceptButton);

                return;
            }

            var viewModel = new ConversionConfirmationViewModel(_app)
            {
                FromCurrencyViewModel = FromViewModel.CurrencyViewModel,
                ToCurrencyViewModel = ToViewModel.CurrencyViewModel,
                FromSource = FromCurrencyViewModelItem.FromSource,
                ToAddress = ToAddress,
                RedeemFromAddress = RedeemFromAddress,

                BaseCurrencyCode = BaseCurrencyCode,
                QuoteCurrencyCode = QuoteCurrencyCode,
                Amount = FromViewModel.Amount,
                AmountInBase = FromViewModel.AmountInBase,
                TargetAmount = ToViewModel.Amount,
                TargetAmountInBase = ToViewModel.AmountInBase,

                EstimatedPrice = EstimatedPrice,
                EstimatedOrderPrice = _estimatedOrderPrice,
                EstimatedMakerNetworkFee = EstimatedMakerNetworkFee,
                EstimatedTotalNetworkFeeInBase = EstimatedTotalNetworkFeeInBase,
            };

            viewModel.OnSuccess += OnSuccessConvertion;
            
            _ = PopupNavigation.Instance.PushAsync(new ExchangeConfirmationBottomSheet(viewModel));
        }

        private void OnSuccessConvertion(object sender, EventArgs e)
        {
            FromViewModel.SetAmountFromString("0");
            ToViewModel.SetAmountFromString("0");
        }

        protected void ShowMessage(MessageType messageType, RelatedTo element, string text, string tooltipText = null)
        {
            Message.Type = messageType;
            Message.RelatedTo = element;
            Message.Text = text;
            Message.TooltipText = tooltipText;

            this.RaisePropertyChanged(nameof(Message));
        }

        protected void SetAmountToFeeRationWarning(MessageType messageType, RelatedTo element, string text, string tooltipText = null)
        {
            AmountToFeeRatioWarning.Type = messageType;
            AmountToFeeRatioWarning.RelatedTo = element;
            AmountToFeeRatioWarning.Text = text;
            AmountToFeeRatioWarning.TooltipText = tooltipText;

            this.RaisePropertyChanged(nameof(AmountToFeeRatioWarning));
        }
    }
}