using System;
using System.Globalization;
using Atomex.Blockchain.Abstract;
using Atomex.Core;

namespace atomex.ViewModel.TransactionViewModels
{
    public enum TransactionType
    {
        Input,
        Output,
        SwapPayment,
        SwapRedeem,
        SwapRefund
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
        public bool CanBeRemoved { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public TransactionViewModel(IBlockchainTransaction tx, decimal amount, decimal fee)
        {
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Id = Transaction.Id;
            Currency = Transaction.Currency;
            State = Transaction.State;
            Type = GetType(Transaction.Type);
            Amount = amount;

            var netAmount = amount + fee;

            AmountFormat = tx.Currency.Format;
            CurrencyCode = tx.Currency.Name;
            Time = tx.CreationTime ?? DateTime.UtcNow;
            CanBeRemoved = tx.State == BlockchainTransactionState.Failed ||
                           tx.State == BlockchainTransactionState.Pending;

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
            else if (Amount < 0) //tx.Type.HasFlag(BlockchainTransactionType.Output))
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

            if (type.HasFlag(BlockchainTransactionType.Input) &&
                type.HasFlag(BlockchainTransactionType.Output))
                return TransactionType.Output;

            if (type.HasFlag(BlockchainTransactionType.Input))
                return TransactionType.Input;

            //if (type.HasFlag(BlockchainTransactionType.Output))
            return TransactionType.Output;
        }
    }
}