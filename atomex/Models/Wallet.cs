using System.Collections.Generic;

namespace atomex.Models
{
    public class Wallet
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public float PercentInPortfolio { get; set; }
        public string Address { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}