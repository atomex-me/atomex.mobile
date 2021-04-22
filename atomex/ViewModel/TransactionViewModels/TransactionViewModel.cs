using System;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel.TransactionViewModels
{
    public enum TransactionType
    {
        Input,
        Output,
        SwapPayment,
        SwapRedeem,
        SwapRefund,
        TokenCall,
        SwapCall,
        TokenApprove
    }

    public class TransactionViewModel : BaseViewModel
    {
        public IBlockchainTransaction Transaction { get; }
        public string Id { get; set; }
        public Currency Currency { get; set; }
        public BlockchainTransactionState State { get; set; }
        //public BlockchainTransactionType Type { get; set; }
        public TransactionType Type { get; set; }

        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Fee { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public string TxExplorerUri => $"{Currency.TxExplorerUri}{Id}";
        public string From { get; set; }
        public string To { get; set; }
        public bool CanBeRemoved { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        private IToastService ToastService;

        public CurrencyViewModel CurrencyViewModel;

        public TransactionViewModel()
        {
        }

        public TransactionViewModel(IBlockchainTransaction tx, decimal amount, decimal fee)
        {
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Id = Transaction.Id;
            Currency = Transaction.Currency;
            State = Transaction.State;
            Type = GetType(Transaction.Type);
            //Type = Transaction.Type;
            Amount = amount;

            ToastService = DependencyService.Get<IToastService>();

            var netAmount = amount + fee;

            AmountFormat = tx.Currency.Format;
            CurrencyCode = tx.Currency.Name;
            Time = tx.CreationTime ?? DateTime.UtcNow;
            CanBeRemoved = tx.State == BlockchainTransactionState.Failed ||
                           tx.State == BlockchainTransactionState.Pending ||
                           tx.State == BlockchainTransactionState.Unknown ||
                           tx.State == BlockchainTransactionState.Unconfirmed;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapPayment))
            {
                Description = $"Swap payment {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.SwapRefund))
            {
                Description = $"Swap refund {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.SwapRedeem))
            {
                Description = $"Swap redeem {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.TokenApprove))
            {
                Description = $"Token approve";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.TokenCall))
            {
                Description = $"Token call";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.SwapCall))
            {
                Description = $"Token swap call";
            }
            else if (Amount <= 0) //tx.Type.HasFlag(BlockchainTransactionType.Output))
            {
                Description = $"Sent {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else if (Amount > 0) //tx.Type.HasFlag(BlockchainTransactionType.Input)) // has outputs
            {
                Description = $"Received {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else
            {
                Description = "Unknown transaction";
            }
        }

        public TransactionType GetType(BlockchainTransactionType type)
        {
            if (type.HasFlag(BlockchainTransactionType.SwapPayment))
                return TransactionType.SwapPayment;

            if (type.HasFlag(BlockchainTransactionType.SwapRedeem))
                return TransactionType.SwapRedeem;

            if (type.HasFlag(BlockchainTransactionType.SwapRefund))
                return TransactionType.SwapRefund;

            if (type.HasFlag(BlockchainTransactionType.SwapCall))
                return TransactionType.SwapCall;

            if (type.HasFlag(BlockchainTransactionType.TokenCall))
                return TransactionType.TokenCall;

            if (type.HasFlag(BlockchainTransactionType.TokenApprove))
                return TransactionType.TokenApprove;

            if (type.HasFlag(BlockchainTransactionType.Input) &&
                type.HasFlag(BlockchainTransactionType.Output))
                return TransactionType.Output;

            if (type.HasFlag(BlockchainTransactionType.Input))
                return TransactionType.Input;

            //if (type.HasFlag(BlockchainTransactionType.Output))
            return TransactionType.Output;
        }

        private ICommand _copyTxIdCommand;
        public ICommand CopyTxIdCommand => _copyTxIdCommand ??= new Command(async () => await OnCopyIdButtonClicked());

        private ICommand _copyFromAddressCommand;
        public ICommand CopyFromAddressCommand => _copyFromAddressCommand ??= new Command(async () => await OnCopyFromAddressButtonClicked());

        private ICommand _copyToAddressCommand;
        public ICommand CopyToAddressCommand => _copyToAddressCommand ??= new Command(async () => await OnCopyToAddressButtonClicked());

        private ICommand _showInExplorerCommand;
        public ICommand ShowInExplorerCommand => _showInExplorerCommand ??= new Command(() => OnShowInExplorerClicked());

        private ICommand _deleteTxCommand;
        public ICommand DeleteTxCommand => _deleteTxCommand ??= new Command(async () => await OnDeleteTxButtonClicked());

        private async Task OnDeleteTxButtonClicked()
        {
            if (CurrencyViewModel != null)
            {
                var res = await Application.Current.MainPage.DisplayAlert(AppResources.Warning, AppResources.RemoveTxWarning, AppResources.AcceptButton, AppResources.CancelButton);
                if (!res) return;
                CurrencyViewModel.RemoveTransactonAsync(Id);
            }
        }

        private void OnShowInExplorerClicked()
        {
            Launcher.OpenAsync(new Uri(TxExplorerUri));
        }

        private async Task OnCopyIdButtonClicked()
        {
            try
            { 
                await Clipboard.SetTextAsync(Id);
                ToastService?.Show(AppResources.TransactionIdCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch(Exception)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        private async Task OnCopyFromAddressButtonClicked()
        {
            try
            {
                await Clipboard.SetTextAsync(From);
                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        private async Task OnCopyToAddressButtonClicked()
        {
            try
            {
                await Clipboard.SetTextAsync(To);
                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }
    }
}