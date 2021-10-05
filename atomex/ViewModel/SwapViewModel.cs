using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using atomex.ViewModel;
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
                        StateDescription = "Do not close the app until the swap is completed";
                        break;
                    case SwapCompactState.Completed:
                        State = "Completed";
                        StateDescription = "You can close Atomex app now";
                        break;
                    case SwapCompactState.Canceled:
                        State = "Canceled";
                        StateDescription = "Swap was canceled by you";
                        break;
                    case SwapCompactState.Refunded:
                        State = "Refunded";
                        StateDescription = "Lock time has passed (here the reason for a refund goes)";
                        break;
                    case SwapCompactState.Unsettled:
                        State = "Unsettled";
                        StateDescription = "Here the reason or description goes";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                OnPropertyChanged(nameof(CompactState));
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

                Status = SwapDetailingStatus.Initialization.ToString();
                foreach (var item in DetailingInfo)
                {
                    switch (item.Status)
                    {
                        case SwapDetailingStatus.Initialization:
                            if (item.IsCompleted)
                                Status = SwapDetailingStatus.Exchanging.ToString();
                            InitStatusMessages.Add(item.Description);
                            break;
                        case SwapDetailingStatus.Exchanging:
                            if (item.IsCompleted)
                                Status = SwapDetailingStatus.Completion.ToString();
                            ExchangeStatusMessages.Add(item.Description);
                            break;
                        case SwapDetailingStatus.Completion:
                            CompletionStatusMessages.Add(item.Description);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                OnPropertyChanged(nameof(DetailingInfo));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(InitStatusMessages));
                OnPropertyChanged(nameof(ExchangeStatusMessages));
                OnPropertyChanged(nameof(CompletionStatusMessages));
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
    }
}

