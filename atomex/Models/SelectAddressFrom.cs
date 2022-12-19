using System.ComponentModel;

namespace atomex.ViewModels.Abstract
{
    public enum SelectAddressFrom
    {
        [Description("Init")] Init,
        [Description("Change")] Change,
        [Description("InitSearch")] InitSearch,
        [Description("ChangeSearch")] ChangeSearch
    }
}