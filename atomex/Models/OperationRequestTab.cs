using System.ComponentModel;

namespace atomex.Models
{
    public enum OperationRequestTab
    {
        [Description("Preview")] Preview,
        [Description("Raw")] Raw,
        [Description("Bytes")] Bytes
    }
}