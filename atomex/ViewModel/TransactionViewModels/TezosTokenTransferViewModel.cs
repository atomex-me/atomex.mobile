﻿using System;
using Atomex;
using Atomex.Common;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using atomex.Resources;

namespace atomex.ViewModel.TransactionViewModels
{
    public class TezosTokenTransferViewModel : TransactionViewModel
    {
        public const int MaxAmountDecimals = 9;
        public string TxHash => Id.Split('/')[0];
        private readonly TezosConfig _tezosConfig;
        public string Alias { get; set; }

        public TezosTokenTransferViewModel(TokenTransfer tx, TezosConfig tezosConfig)
        {
            _tezosConfig = tezosConfig;

            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Id = tx.Id;
            State = Transaction.State;
            Type = Transaction.Type;
            From = tx.From;
            To = tx.To;
            CurrencyCode = tx.Token.Symbol;
            Amount = GetAmount(tx);
            AmountFormat = $"F{Math.Min(tx.Token.Decimals, MaxAmountDecimals)}";
            CurrencyCode = tx.Token.Symbol;
            Time = tx.CreationTime ?? DateTime.UtcNow;
            Alias = tx.GetAlias();
            Direction = Amount <= 0 ? AppResources.ToLabel : AppResources.FromLabel;

            TxExplorerUri = $"{_tezosConfig.TxExplorerUri}{Id}";
            AddressExplorerUri = $"{_tezosConfig.AddressExplorerUri}";

            Description = GetDescription(
                type: tx.Type,
                amount: Amount,
                netAmount: Amount,
                amountDigits: tx.Token.Decimals,
                currencyCode: tx.Token.Symbol);
        }

        private static decimal GetAmount(TokenTransfer tx)
        {
            if (tx.Amount.TryParseWithRound(tx.Token.Decimals, out var amount))
            {
                var sign = tx.Type.HasFlag(BlockchainTransactionType.Input)
                    ? 1
                    : -1;


                return sign * amount;
            }

            return 0;
        }
    }
}
