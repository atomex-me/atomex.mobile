using System;
using System.Collections.Generic;
using System.Linq;
using Atomex.Core;
using Microsoft.Extensions.FileProviders;
using Netezos.Forging.Models;

namespace atomex.Models
{
    public class Transaction
    {
        public long Amount { get; set; }
        public string Destination { get; set; }
        public Parameters Parameters { get; set; }
        public string Source { get; set; }
        public decimal Fee { get; set; }
        public int Counter { get; set; }
        public decimal GasLimit { get; set; }
        public decimal StorageLimit { get; set; }
        public decimal SumFee => StorageLimit + GasLimit + Fee;
    }
}