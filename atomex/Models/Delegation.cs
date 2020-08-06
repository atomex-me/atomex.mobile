using System;
using Atomex.Blockchain.Tezos;

namespace atomex.Models
{
    public class Delegation
    {
        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public string BbUri { get; set; }
        public DateTime DelegationTime { get; set; }
        public string Status { get; set; }
    }
}
