using System.ComponentModel;

namespace atomex.Models
{
    public class SnackbarMessage
    {
        public enum MessageType
        {
            [Description("Warning")]
            Warning,
            [Description("Error")]
            Error,
            [Description("Success")]
            Success,
            [Description("Regular")]
            Regular
        }

        public MessageType Type { get; set; }
        public string Text { get; set; }
        public string Button { get; set; }
    }
}
