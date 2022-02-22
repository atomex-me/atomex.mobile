namespace atomex.Models
{
    public enum MessageType
    {
        Warning,
        Error,
        Regular
    }

    public enum RelatedTo
    {
        Amount,
        Fee,
        Address,
        Core
    }

    public class Message
    {
        public MessageType Type { get; set; }
        public RelatedTo RelatedTo { get; set; }
        public string Text { get; set; }
        public string TooltipText { get; set; }
    }
}
