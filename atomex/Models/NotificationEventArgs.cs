using System;

namespace atomex.Models
{
    public class NotificationEventArgs : EventArgs
    {
        public bool Alert { get; set; }
        public long SwapId { get; set; }
        public string Currency { get; set; }
        public string TxId { get; set; }
        public string PushType { get; set; }
    }
}
