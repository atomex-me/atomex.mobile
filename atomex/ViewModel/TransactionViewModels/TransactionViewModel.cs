using System;
using System.Globalization;
using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Core;

namespace atomex.ViewModel.TransactionViewModels
{
    public class TransactionViewModel : BaseViewModel
    {
        public event EventHandler<TransactionEventArgs> UpdateClicked;
        public event EventHandler<TransactionEventArgs> RemoveClicked;

        public IBlockchainTransaction Transaction { get; }
        public string Id { get; set; }
        public Currency Currency { get; set; }
        public BlockchainTransactionState State { get; set; }
        public BlockchainTransactionType Type { get; set; }

        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Fee { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public string TxExplorerUri => $"{Currency.TxExplorerUri}{Id}";
        public bool CanBeRemoved { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public TransactionViewModel(IBlockchainTransaction tx, decimal amount)
        {
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Id = Transaction.Id;
            Currency = Transaction.Currency;
            State = Transaction.State;
            Type = Transaction.Type;
            Amount = amount;

            AmountFormat = tx.Currency.Format;
            CurrencyCode = tx.Currency.Name;
            Time = tx.CreationTime ?? DateTime.UtcNow;
            CanBeRemoved = tx.State == BlockchainTransactionState.Failed ||
                           tx.State == BlockchainTransactionState.Pending;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapPayment))
            {
                Description = $"Swap payment {Math.Abs(Amount).ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.SwapRefund))
            {
                Description = $"Swap refund {Math.Abs(Amount).ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.SwapRedeem))
            {
                Description = $"Swap redeem {Math.Abs(Amount).ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
            }
            else if (Amount < 0) //tx.Type.HasFlag(BlockchainTransactionType.Output))
            {
                Description = $"Sent {Math.Abs(Amount).ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
            }
            else if (Amount >= 0) //tx.Type.HasFlag(BlockchainTransactionType.Input)) // has outputs
            {
                Description = $"Received {Math.Abs(Amount).ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
            }
            else
            {
                Description = "Unknown transaction";
            }
        }
    }
}