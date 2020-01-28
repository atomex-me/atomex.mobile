using System;
using System.Collections.Generic;

namespace atomex
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
                    Currency = "Ethereum",
                    Type = "Output",
                    Amount = 0.003f,
                    From = "eth#8su7s62gk^4hbdj#$7",
                    To = "eth#f4tm9eu@90n3$#kdsfby",
                    CreationTime = new DateTime(2019, 8, 13, 12, 32, 30)
                },
                new Transaction
                {
                    Currency = "Ethereum",
                    Type = "Input",
                    Amount = 0.739f,
                    From = "eth#ghj#j@jNB$>hgj%bnb*jhbdjs",
                    To = "eth#8su7s62gk^4hbdj#$7",
                    CreationTime = new DateTime(2019, 9, 21, 16, 09, 30)
                },
                new Transaction
                {
                    Currency = "Tezos",
                    Type = "Output",
                    Amount = 18f,
                    From = "xtz#h6s02ks92d9@jd3$ss0l",
                    To = "xtz#93ih38d7sn4@khb$720JKdbvn^67",
                    CreationTime = new DateTime(2019, 3, 13, 02, 19, 32)
                }
            };
        }
    }
}
