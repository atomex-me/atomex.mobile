using System;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Resources;
using atomex.Services;
using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel.TransactionViewModels
{
    //public enum TransactionType
    //{
    //    Input,
    //    Output,
    //    SwapPayment,
    //    SwapRedeem,
    //    SwapRefund,
    //    TokenCall,
    //    SwapCall,
    //    TokenApprove
    //}

    public class TransactionViewModel : BaseViewModel
    {
        public event EventHandler<TransactionEventArgs> RemoveClicked;

        public IBlockchainTransaction Transaction { get; set; }
        public string Id { get; set; }
        public CurrencyConfig Currency { get; set; }
        public BlockchainTransactionState State { get; set; }
        public BlockchainTransactionType Type { get; set; }
        //public TransactionType Type { get; set; }

        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Fee { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public string TxExplorerUri { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public bool CanBeRemoved { get; set; }


        private IToastService ToastService;

        public TransactionViewModel()
        {
        }

        public TransactionViewModel(IBlockchainTransaction tx, CurrencyConfig currencyConfig, decimal amount, decimal fee)
        {
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Id = Transaction.Id;
            Currency = currencyConfig;
            State = Transaction.State;
            //Type = GetType(Transaction.Type);
            Type = Transaction.Type;
            Amount = amount;

            TxExplorerUri = $"{Currency.TxExplorerUri}{Id}";

            ToastService = DependencyService.Get<IToastService>();

            var netAmount = amount + fee;
        
            AmountFormat = currencyConfig.Format;
            CurrencyCode = currencyConfig.Name;
            Time = tx.CreationTime ?? DateTime.UtcNow;
            CanBeRemoved = tx.State == BlockchainTransactionState.Failed ||
                           tx.State == BlockchainTransactionState.Pending ||
                           tx.State == BlockchainTransactionState.Unknown ||
                           tx.State == BlockchainTransactionState.Unconfirmed;

            Description = GetDescription(
                type: tx.Type,
                amount: Amount,
                netAmount: netAmount,
                amountDigits: currencyConfig.Digits,
                currencyCode: currencyConfig.Name);

            
        }

        public static string GetDescription(
            BlockchainTransactionType type,
            decimal amount,
            decimal netAmount,
            int amountDigits,
            string currencyCode)
        {
            if (type.HasFlag(BlockchainTransactionType.SwapPayment))
            {
                 return $"{AppResources.TxSwapPayment} {Math.Abs(amount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else if (type.HasFlag(BlockchainTransactionType.SwapRefund))
            {
                return $"{AppResources.TxSwapRefund} {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else if (type.HasFlag(BlockchainTransactionType.SwapRedeem))
            {
                return $"{AppResources.TxSwapRedeem} {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else if (type.HasFlag(BlockchainTransactionType.TokenApprove))
            {
                return $"{AppResources.TxTokenApprove}";
            }
            else if (type.HasFlag(BlockchainTransactionType.TokenCall))
            {
                return $"{AppResources.TxTokenCall}";
            }
            else if (type.HasFlag(BlockchainTransactionType.SwapCall))
            {
                return $"{AppResources.TxSwapCall}";
            }
            else if (amount <= 0)
            {
                return $"{AppResources.TxSent} {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else if (amount > 0)
            {
                return $"{AppResources.TxReceived} {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";
            }
            else
            {
                return $"{AppResources.TxUnknown}";
            }
        }

        //public static TransactionType GetType(BlockchainTransactionType type)
        //{
        //    if (type.HasFlag(BlockchainTransactionType.SwapPayment))
        //        return TransactionType.SwapPayment;

        //    if (type.HasFlag(BlockchainTransactionType.SwapRedeem))
        //        return TransactionType.SwapRedeem;

        //    if (type.HasFlag(BlockchainTransactionType.SwapRefund))
        //        return TransactionType.SwapRefund;

        //    if (type.HasFlag(BlockchainTransactionType.SwapCall))
        //        return TransactionType.SwapCall;

        //    if (type.HasFlag(BlockchainTransactionType.TokenCall))
        //        return TransactionType.TokenCall;

        //    if (type.HasFlag(BlockchainTransactionType.TokenApprove))
        //        return TransactionType.TokenApprove;

        //    if (type.HasFlag(BlockchainTransactionType.Input) &&
        //        type.HasFlag(BlockchainTransactionType.Output))
        //        return TransactionType.Output;

        //    if (type.HasFlag(BlockchainTransactionType.Input))
        //        return TransactionType.Input;

        //    return TransactionType.Output;
        //}

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
            var res = await Application.Current.MainPage.DisplayAlert(AppResources.Warning, AppResources.RemoveTxWarning, AppResources.AcceptButton, AppResources.CancelButton);

            if (!res) return;
                RemoveClicked?.Invoke(this, new TransactionEventArgs(Transaction));
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

                if (ToastService == null)
                    ToastService = DependencyService.Get<IToastService>();

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

                if (ToastService == null)
                    ToastService = DependencyService.Get<IToastService>();

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

                if (ToastService == null)
                    ToastService = DependencyService.Get<IToastService>();

                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }
    }
}