using System.ComponentModel;

namespace atomex.Models
{
    public enum SelectAddressMode
    {
        [Description("SendFrom")] SendFrom,
        [Description("ReceiveTo")] ReceiveTo,
        [Description("ChangeRedeemAddress")] ChangeRedeemAddress,
        [Description("EnterExternalAddress")] EnterExternalAddress,
        [Description("ChooseMyAddress")] ChooseMyAddress,
        [Description("Connect")] Connect
    }
}