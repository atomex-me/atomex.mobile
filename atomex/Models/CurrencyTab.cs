using System.ComponentModel;

namespace atomex.Models
{
    public enum CurrencyTab
    {
        [Description("Activity")] Activity,
        [Description("Collectibles")] Collectibles,
        [Description("Addresses")] Addresses,
        [Description("Delegations")] Delegations,
        [Description("Tokens")] Tokens,
        [Description("Details")] Details,
        [Description("Dapps")] Dapps
    }
}