using System;
using System.Collections.Generic;

namespace atomex.Models
{
    public static class TransactionData 
    {
        public static List<Transaction> Transactions { get; set; }

        static TransactionData()
        {
            Transactions = new List<Transaction>
            {
                new Transaction
                {
                    Action = "To",
                    Amount = 0.003f,
                    Address = "btc#ji82m9eu@9043n3$kh#kds?dj3",
                    Date = new DateTime(2019, 8, 13, 12, 32, 30)
                },
                new Transaction
                {
                    Action = "From",
                    Amount = 0.739f,
                    Address = "eth#ghj#j@jNB$>hgj%bnb*jhbdjs",
                    Date = new DateTime(2019, 9, 21, 16, 09, 30)
                },
                new Transaction
                {
                    Action = "From",
                    Amount = 18f,
                    Address = "xtz#93ih38d7sn4@khb$720JKdbvn^67",
                    Date = new DateTime(2019, 3, 13, 02, 19, 32)
                }
            };
        }
    }
}
