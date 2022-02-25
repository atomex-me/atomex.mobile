using System.ComponentModel;

namespace atomex.Models
{
    public enum MessageType
    {
        [Description("Warning")]
        Warning,
        [Description("Error")]
        Error,
        [Description("Regular")]
        Regular
    }

    public enum RelatedTo
    {
        [Description("Amount")]
        Amount,
        [Description("Fee")]
        Fee,
        [Description("Address")]
        Address,
        [Description("All")]
        All
    }

    public class Message
    {
        public MessageType Type { get; set; }
        public RelatedTo RelatedTo { get; set; }
        public string Text { get; set; }
        public string TooltipText { get; set; }
    }
}
