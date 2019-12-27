using System;
using System.Collections.Generic;
using atomex.Models;
using System.Linq;

namespace atomex.ViewModel
{
    public class TransactionsViewModel : BaseViewModel
    {
        public List<Transaction> Transactions { get => TransactionData.Transactions; }

        public List<Transaction> GetSortedList() {
            if (Transactions != null)
            {
                return Transactions.OrderByDescending(transaction => transaction.Date).ToList();
            }
            return null;
        }
    }
}
