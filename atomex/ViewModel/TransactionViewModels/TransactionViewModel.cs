using System;
using System.Reactive;
using atomex.Resources;
using atomex.Services;
using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using ReactiveUI;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModel.TransactionViewModels
{
    public class TransactionViewModel : BaseViewModel
    {
        public event EventHandler<TransactionEventArgs> RemoveClicked;

        public IBlockchainTransaction Transaction { get; set; }
        public string Id { get; set; }
        public CurrencyConfig Currency { get; set; }
        public BlockchainTransactionState State { get; set; }
        public BlockchainTransactionType Type { get; set; }

        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Fee { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public string TxExplorerUri { get; set; }
        public string AddressExplorerUri { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Direction { get; set; }
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
            Type = Transaction.Type;
            Amount = amount;
            Direction = amount <= 0 ? AppResources.ToLabel : AppResources.FromLabel;

            TxExplorerUri = $"{Currency.TxExplorerUri}{Id}";
            AddressExplorerUri = $"{Currency.AddressExplorerUri}";

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

        private ReactiveCommand<string, Unit> _copyTxIdCommand;
        public ReactiveCommand<string, Unit> CopyTxIdCommand => _copyTxIdCommand ??= ReactiveCommand.CreateFromTask<string>(async (value) =>
        {
            try
            {
                await Clipboard.SetTextAsync(value);

                if (ToastService == null)
                    ToastService = DependencyService.Get<IToastService>();

                ToastService?.Show(AppResources.TransactionIdCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        });

        private ReactiveCommand<string, Unit> _copyAddressCommand;
        public ReactiveCommand<string, Unit> CopyAddressCommand => _copyAddressCommand ??= ReactiveCommand.CreateFromTask<string>(async (value) =>
        {
            try
            {
                await Clipboard.SetTextAsync(value);

                if (ToastService == null)
                    ToastService = DependencyService.Get<IToastService>();

                ToastService?.Show(AppResources.AddressCopied, ToastPosition.Top, Application.Current.RequestedTheme.ToString());
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        });

        private ReactiveCommand<Unit, Unit> _showTxInExplorerCommand;
        public ReactiveCommand<Unit, Unit> ShowTxInExplorerCommand => _showTxInExplorerCommand ??= ReactiveCommand.CreateFromTask(() => Launcher.OpenAsync(new Uri(TxExplorerUri)));

        private ReactiveCommand<string, Unit> _showAddressInExplorerCommand;
        public ReactiveCommand<string, Unit> ShowAddressInExplorerCommand => _showAddressInExplorerCommand ??= ReactiveCommand.CreateFromTask<string>((value) => Launcher.OpenAsync(new Uri($"{AddressExplorerUri}{value}")));

        private ReactiveCommand<Unit, Unit> _deleteTxCommand;
        public ReactiveCommand<Unit, Unit> DeleteTxCommand => _deleteTxCommand ??= ReactiveCommand.CreateFromTask(async () =>
        {
            var res = await Application.Current.MainPage.DisplayAlert(AppResources.Warning, AppResources.RemoveTxWarning, AppResources.AcceptButton, AppResources.CancelButton);

            if (!res) return;
            RemoveClicked?.Invoke(this, new TransactionEventArgs(Transaction));
        });
    }
}