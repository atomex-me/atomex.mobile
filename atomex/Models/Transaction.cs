using System;

namespace atomex
{
    public class Transaction
    {
        public string Id { get; set; }
        public string Currency { get; set; }
        public string Type { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public float Amount { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
