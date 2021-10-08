using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using atomex.Resources;
using atomex.ViewModel;
using Atomex.Core;
using Xamarin.Forms;
using static Atomex.ViewModels.Helpers;

namespace atomex
{

    public enum SwapCompactState
    {
        Canceled,
        InProgress,
        Completed,
        Refunded,
        Unsettled
    }

    public enum SwapMode
    {
        Initiator,
        CounterParty
    }

    public class SwapViewModel : BaseViewModel
    {
        public string Id { get; set; }

        private SwapCompactState _compactState;
        public SwapCompactState CompactState
        {
            get => _compactState;
            set
            {
                switch (value)
                {
                    case SwapCompactState.InProgress:
                        State = "In Progress";
                        StateDescription = AppResources.SwapInProgressDesc;
                        break;
                    case SwapCompactState.Completed:
                        State = "Completed";
                        StateDescription = AppResources.SwapCompletedDesc;
                        break;
                    case SwapCompactState.Canceled:
                        State = "Canceled";
                        StateDescription = AppResources.SwapCanceledDesc;
                        break;
                    case SwapCompactState.Refunded:
                        State = "Refunded";
                        StateDescription = AppResources.SwapRefundedDesc;
                        break;
                    case SwapCompactState.Unsettled:
                        State = "Unsettled";
                        StateDescription = AppResources.SwapUnsettledDesc;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                OnPropertyChanged(nameof(CompactState));
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(StateDescription));
            }
        }

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
                                    _status = SwapDetailingStatus.Exchanging.ToString();
                                _initStatusMessages.Add(item.Description);
                                break;
                            case SwapDetailingStatus.Exchanging:
                                if (item.IsCompleted)
                                    _status = SwapDetailingStatus.Completion.ToString();
                                _exchangeStatusMessages.Add(item.Description);
                                break;
                            case SwapDetailingStatus.Completion:
                                _status = SwapDetailingStatus.Completion.ToString();
                                _completionStatusMessages.Add(item.Description);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    OnPropertyChanged(nameof(DetailingInfo));
                    OnPropertyChanged(nameof(Status));
                });
            }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private ObservableCollection<string> _initStatusMessages;
        public ObservableCollection<string> InitStatusMessages
        {
            get => _initStatusMessages;
            set { _initStatusMessages = value; OnPropertyChanged(nameof(InitStatusMessages)); }
        }

        private ObservableCollection<string> _exchangeStatusMessages;
        public ObservableCollection<string> ExchangeStatusMessages
        {
            get => _exchangeStatusMessages;
            set { _exchangeStatusMessages = value; OnPropertyChanged(nameof(ExchangeStatusMessages)); }
        }

        private ObservableCollection<string> _completionStatusMessages;
        public ObservableCollection<string> CompletionStatusMessages
        {
            get => _completionStatusMessages;
            set { _completionStatusMessages = value; OnPropertyChanged(nameof(CompletionStatusMessages)); }
        }

        public string State { get; set; }

        public string StateDescription { get; set; }

        public SwapViewModel()
        {
            _initStatusMessages = new ObservableCollection<string>();
            _exchangeStatusMessages = new ObservableCollection<string>();
            _completionStatusMessages = new ObservableCollection<string>();
        }

        public void UpdateSwap(Swap swap)
        {
            CompactState = SwapViewModelFactory.CompactStateBySwap(swap);
            DetailingInfo = GetSwapDetailingInfo(swap).ToList();
        }

        private void ClearStatusMessages()
        {
            _initStatusMessages.Clear();
            _exchangeStatusMessages.Clear();
            _completionStatusMessages.Clear();
        }
    }
}

