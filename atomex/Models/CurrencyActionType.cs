using System.ComponentModel;

namespace atomex.Models
{
    public enum CurrencyActionType
    {
        [Description("Show")] Show,
        [Description("Send")] Send,
        [Description("Receive")] Receive
    }
}