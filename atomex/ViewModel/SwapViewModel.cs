using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using atomex.Models;
using atomex.Resources;
using atomex.ViewModel;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Xamarin.Essentials;
using Xamarin.Forms;
using static Atomex.ViewModels.Helpers;

namespace atomex
{
    public enum SwapMode
    {
        Initiator,
        CounterParty
    }

    public class SwapViewModel : BaseViewModel
    {
        public string Id { get; set; }

        public SwapMode Mode { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();

        public decimal FromAmount { get; set; }
        public string FromAmountFormat { get; set; }
        public string FromCurrencyCode { get; set; }

        public decimal ToAmount { get; set; }
        public string ToAmountFormat { get; set; }
        public string ToCurrencyCode { get; set; }

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
                
                Device.BeginInvokeOnMainThread(() =>
                {
                    ClearStatusMessages();

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
                                if ((!item.IsCompleted && State == "Canceled") ||
                                    (!item.IsCompleted && State == "Refunded") ||
                                    (!item.IsCompleted && State == "Unsettled"))
                                    _initStatusDesc = AppResources.InitFailure;
                                _initStatusMessages.Add(new SwapDetailingMessage {
                                    Message = item.Description,
                                    ExplorerLink = item.ExplorerLink });
                                break;
                            case SwapDetailingStatus.Exchanging:
                                if (item.IsCompleted)
                                {
                                    _status = SwapDetailingStatus.Completion.ToString();
                                    _exchangeStatusDesc = AppResources.PaymentsConfirmed;
                                }
                                if ((!item.IsCompleted && State == "Canceled") ||
                                    (!item.IsCompleted && State == "Refunded") ||
                                    (!item.IsCompleted && State == "Unsettled"))
                                    _exchangeStatusDesc = AppResources.FailedPayment;
                                _exchangeStatusMessages.Add(new SwapDetailingMessage {
                                    Message = item.Description,
                                    ExplorerLink = item.ExplorerLink });
                                break;
                            case SwapDetailingStatus.Completion:
                                _status = SwapDetailingStatus.Completion.ToString();
                                if (State == "Completed")
                                    _completionStatusDesc = string.Format(CultureInfo.InvariantCulture, AppResources.CurrencyReceived, ToCurrencyCode);
                                if ((!item.IsCompleted && State == "Canceled") ||
                                   (!item.IsCompleted && State == "Refunded") ||
                                   (!item.IsCompleted && State == "Unsettled"))
                                    _exchangeStatusDesc = AppResources.FailedRedeem;
                                _completionStatusMessages.Add(new SwapDetailingMessage {
                                    Message = item.Description,
                                    ExplorerLink = item.ExplorerLink });
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    OnPropertyChanged(nameof(DetailingInfo));
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

        private ObservableCollection<SwapDetailingMessage> _initStatusMessages;
        public ObservableCollection<SwapDetailingMessage> InitStatusMessages
        {
            get => _initStatusMessages;
            set { _initStatusMessages = value; OnPropertyChanged(nameof(InitStatusMessages)); }
        }

        private ObservableCollection<SwapDetailingMessage> _exchangeStatusMessages;
        public ObservableCollection<SwapDetailingMessage> ExchangeStatusMessages
        {
            get => _exchangeStatusMessages;
            set { _exchangeStatusMessages = value; OnPropertyChanged(nameof(ExchangeStatusMessages)); }
        }

        private ObservableCollection<SwapDetailingMessage> _completionStatusMessages;
        public ObservableCollection<SwapDetailingMessage> CompletionStatusMessages
        {
            get => _completionStatusMessages;
            set { _completionStatusMessages = value; OnPropertyChanged(nameof(CompletionStatusMessages)); }
        }

        private string _state;
        public string State
        { 
            get => _state;
            set { _state = value; OnPropertyChanged(nameof(CompletionStatusMessages)); }
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

        public IAccount Account { get; set; }

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
            _initStatusMessages = new ObservableCollection<SwapDetailingMessage>();
            _exchangeStatusMessages = new ObservableCollection<SwapDetailingMessage>();
            _completionStatusMessages = new ObservableCollection<SwapDetailingMessage>();
        }

        public void UpdateSwap(Swap swap)
        {
            SetState(swap);
            DetailingInfo = GetSwapDetailingInfo(swap, Account).ToList();
        }

        private void ClearStatusMessages()
        {
            _initStatusMessages.Clear();
            _exchangeStatusMessages.Clear();
            _completionStatusMessages.Clear();
        }

        private ICommand _openInExplorerCommand;
        public ICommand OpenInExplorerCommand => _openInExplorerCommand ??= new Command<string>((value) => OpenInExplorer(value));

        private void OpenInExplorer(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                Launcher.OpenAsync(new Uri(uri));
            }
        }

        private ICommand _expandStatusCommand;
        public ICommand ExpandStatusCommand => _expandStatusCommand ??= new Command<string>((value) => OnStatusClicked(value));

        private void OnStatusClicked(string status)
        {
            if (ExpandedStatus == status)
                ExpandedStatus = string.Empty;
            else
                ExpandedStatus = status;
        }
    }
}

