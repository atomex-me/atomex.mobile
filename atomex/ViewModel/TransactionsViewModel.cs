using System;
using System.Collections.Generic;
using System.Linq;

namespace atomex.ViewModel
{
    public class TransactionsViewModel : BaseViewModel
    {
        public List<Transaction> Transactions { get => TransactionData.Transactions; }

        public List<Transaction> GetTransactionsByCurrency(string currency) {
            if (Transactions != null)
            {
                return Transactions.Where(t => t.Currency == currency).OrderByDescending(t => t.CreationTime).ToList();
            }
            return null;
        }
    }
}
