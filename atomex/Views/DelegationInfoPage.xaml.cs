using Atomex.Blockchain.Tezos;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationInfoPage : ContentPage
    {
        public DelegationInfoPage(Delegation delegation)
        {
            InitializeComponent();
            BindingContext = delegation;
        }
    }
}
