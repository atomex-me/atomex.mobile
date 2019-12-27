using System;

namespace atomex.Models
{
    public class Transaction
    {
        public string Action { get; set; }
        public string Address { get; set; }
        public float Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
