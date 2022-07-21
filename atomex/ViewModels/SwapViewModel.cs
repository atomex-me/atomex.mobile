﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using atomex.Resources;
using Atomex.Abstract;
using Atomex.Core;
using atomex.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using static Atomex.ViewModels.Helpers;

namespace atomex.ViewModels
{
    public enum SwapMode
    {
        Initiator,
        CounterParty
    }

    public class SwapViewModel : BaseViewModel
    {
        private INavigationService _navigationService;

        public string Id { get; set; }

        public SwapMode Mode { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();

        public decimal FromAmount { get; set; }
        public string FromAmountFormat { get; set; }
        public string FromCurrencyCode { get; set; }
        public string FromCurrencyName { get; set; }

        public decimal ToAmount { get; set; }
        public string ToAmountFormat { get; set; }
        public string ToCurrencyCode { get; set; }
        public string ToCurrencyName { get; set; }

        public decimal Price { get; set; }
        public string PriceFormat { get; set; }

        private List<SwapDetailingInfo> _detailingInfo;
        public List<SwapDetailingInfo> DetailingInfo
        {
            get => _detailingInfo;
            set
            {
                _detailingInfo = value;

                if (_detailingInfo == null)
                    return;

                List<SwapDetailingInfo> initMessages = new List<SwapDetailingInfo>();
                List<SwapDetailingInfo> exchangeMessages = new List<SwapDetailingInfo>();
                List<SwapDetailingInfo> completionMessages = new List<SwapDetailingInfo>();

                _status = SwapDetailingStatus.Initialization.ToString();
                foreach (var item in DetailingInfo)
                {
                    switch (item.Status)
                    {
                        case SwapDetailingStatus.Initialization:
                            if (item.IsCompleted)
                            {
                                _status = SwapDetailingStatus.Exchanging.ToString();
                                _initStatusDesc = AppResources.InitCompleted;
                            }
                            else if ((!item.IsCompleted && State == "Canceled") ||
                                (!item.IsCompleted && State == "Unsettled"))
                                _initStatusDesc = AppResources.InitFailure;
                            else if (!item.IsCompleted && State == "Refunded")
                                _initStatusDesc = string.Format(CultureInfo.InvariantCulture, AppResources.CurrencyRefunded, FromCurrencyName);
                            else if (!item.IsCompleted && State == "In Progress")
                                _initStatusDesc = AppResources.WaitingInit;
                            item.IsCompleted = true;
                            initMessages.Add(item);
                            break;
                        case SwapDetailingStatus.Exchanging:
                            if (item.IsCompleted)
                            {
                                _status = SwapDetailingStatus.Completion.ToString();
                                _exchangeStatusDesc = AppResources.PaymentsConfirmed;
                            }
                            else if ((!item.IsCompleted && State == "Canceled") ||
                                (!item.IsCompleted && State == "Unsettled"))
                                _exchangeStatusDesc = AppResources.FailedPayment;
                            else if (!item.IsCompleted && State == "Refunded")
                                _exchangeStatusDesc = string.Format(CultureInfo.InvariantCulture, AppResources.CurrencyRefunded, FromCurrencyName);
                            else if (!item.IsCompleted && State == "In Progress")
                                _exchangeStatusDesc = AppResources.WaitingForPayment;
                            item.IsCompleted = true;
                            exchangeMessages.Add(item);
                            break;
                        case SwapDetailingStatus.Completion:
                            _status = SwapDetailingStatus.Completion.ToString();
                            if (State == "Completed")
                                _completionStatusDesc = string.Format(CultureInfo.InvariantCulture, AppResources.CurrencyReceived, ToCurrencyName);
                            else if ((!item.IsCompleted && State == "Canceled") ||
                                (!item.IsCompleted && State == "Unsettled"))
                                _completionStatusDesc = AppResources.FailedRedeem;
                            else if (!item.IsCompleted && State == "Refunded")
                                _completionStatusDesc = string.Format(CultureInfo.InvariantCulture, AppResources.CurrencyRefunded, FromCurrencyName);
                            else if (!item.IsCompleted && State == "In Progress")
                                _completionStatusDesc = AppResources.WaitingForRedeem;
                            item.IsCompleted = true;
                            completionMessages.Add(item);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (State != "Completed")
                {
                    if (_status == SwapDetailingStatus.Initialization.ToString())
                        initMessages.LastOrDefault().IsCompleted = false;
                    else if (_status == SwapDetailingStatus.Exchanging.ToString())
                        exchangeMessages.LastOrDefault().IsCompleted = false;
                    else if (_status == SwapDetailingStatus.Completion.ToString())
                        completionMessages.LastOrDefault().IsCompleted = false;
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    ClearStatusMessages();

                    initMessages.ForEach(mes => _initStatusMessages.Add(mes));
                    exchangeMessages.ForEach(mes => _exchangeStatusMessages.Add(mes));
                    completionMessages.ForEach(mes => _completionStatusMessages.Add(mes));

                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(InitStatusDesc));
                    OnPropertyChanged(nameof(ExchangeStatusDesc));
                    OnPropertyChanged(nameof(CompletionStatusDesc));
                });
            }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private string _expandedStatus;
        public string ExpandedStatus
        {
            get => _expandedStatus;
            set { _expandedStatus = value; OnPropertyChanged(nameof(ExpandedStatus)); }
        }

        private string _initStatusDesc;
        public string InitStatusDesc
        {
            get => _initStatusDesc;
            set { _initStatusDesc = value; OnPropertyChanged(nameof(InitStatusDesc)); }
        }

        private string _exchangeStatusDesc;
        public string ExchangeStatusDesc
        {
            get => _exchangeStatusDesc;
            set { _exchangeStatusDesc = value; OnPropertyChanged(nameof(ExchangeStatusDesc)); }
        }

        private string _completionStatusDesc;
        public string CompletionStatusDesc
        {
            get => _completionStatusDesc;
            set { _completionStatusDesc = value; OnPropertyChanged(nameof(CompletionStatusDesc)); }
        }

        private ObservableCollection<SwapDetailingInfo> _initStatusMessages;
        public ObservableCollection<SwapDetailingInfo> InitStatusMessages
        {
            get => _initStatusMessages;
            set { _initStatusMessages = value; OnPropertyChanged(nameof(InitStatusMessages)); }
            }

        private ObservableCollection<SwapDetailingInfo> _exchangeStatusMessages;
        public ObservableCollection<SwapDetailingInfo> ExchangeStatusMessages
        {
            get => _exchangeStatusMessages;
            set { _exchangeStatusMessages = value; OnPropertyChanged(nameof(ExchangeStatusMessages)); }
        }

        private ObservableCollection<SwapDetailingInfo> _completionStatusMessages;
        public ObservableCollection<SwapDetailingInfo> CompletionStatusMessages
        {
            get => _completionStatusMessages;
            set { _completionStatusMessages = value; OnPropertyChanged(nameof(CompletionStatusMessages)); }
        }

        private string _state;
        public string State
        { 
            get => _state;
            set { _state = value; OnPropertyChanged(nameof(State)); }
        }

        private string _stateDescription;
        public string StateDescription
        {
            get => _stateDescription;
            set { _stateDescription = value; OnPropertyChanged(nameof(StateDescription)); }
        }

        private string _stateStringI18n;
        public string StateStringI18n
        {
            get => _stateStringI18n;
            set { _stateStringI18n = value; OnPropertyChanged(nameof(StateStringI18n)); }
        }

        public ICurrencies Currencies { get; set; }

        private void SetState(Swap swap)
        {
            if (swap.IsComplete)
            {
                State = "Completed";
                StateDescription = AppResources.SwapCompletedDesc;
                StateStringI18n = AppResources.SwapCompleted;
            }
            else if (swap.IsCanceled)
            {
                State = "Canceled";
                StateDescription = AppResources.SwapCanceledDesc;
                StateStringI18n = AppResources.SwapCanceled;
            }
            else if (swap.IsUnsettled)
            {
                State = "Unsettled";
                StateDescription = AppResources.SwapUnsettledDesc;
                StateStringI18n = AppResources.SwapUnsettled;
            }

            else if (swap.IsRefunded)
            {
                State = "Refunded";
                StateDescription = AppResources.SwapRefundedDesc;
                StateStringI18n = AppResources.SwapRefunded;
            }
            else
            {
                State = "In Progress";
                StateDescription = AppResources.SwapInProgressDesc;
                StateStringI18n = AppResources.SwapInProgress;
            }

            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(StateDescription));
            OnPropertyChanged(nameof(StateStringI18n));
        }

        public SwapViewModel()
        {
            ExpandedStatus = string.Empty;
            InitStatusDesc = string.Empty;
            ExchangeStatusDesc = string.Empty;
            CompletionStatusDesc = string.Empty;
            _initStatusMessages = new ObservableCollection<SwapDetailingInfo>();
            _exchangeStatusMessages = new ObservableCollection<SwapDetailingInfo>();
            _completionStatusMessages = new ObservableCollection<SwapDetailingInfo>();
        }

        public void SetNavigationService(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
        }

        public void UpdateSwap(Swap swap)
        {
            SetState(swap);
            DetailingInfo = GetSwapDetailingInfo(swap, Currencies).ToList();
        }

        private void ClearStatusMessages()
        {
            _initStatusMessages.Clear();
            _exchangeStatusMessages.Clear();
            _completionStatusMessages.Clear();
        }

        private ICommand _openInExplorerCommand;
        public ICommand OpenInExplorerCommand => _openInExplorerCommand ??= new Command<string>((value) =>
        {
            if (!string.IsNullOrEmpty(value))
            {
                Launcher.OpenAsync(new Uri(value));
            }
        });

        private ICommand _expandStatusCommand;
        public ICommand ExpandStatusCommand => _expandStatusCommand ??= new Command<string>((value) =>
            ExpandedStatus = ExpandedStatus == value ? string.Empty : value
        );

        private ICommand _closeBottomSheetCommand;
        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= new Command(() => _navigationService?.CloseBottomSheet());
    }
}

