using System.ComponentModel;

namespace atomex.Models
{
    public class Message
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

        public MessageType Type { get; set; }
        public RelatedTo RelatedElement { get; set; }
        public string Text { get; set; }
        public string TooltipText { get; set; }
    }
}
