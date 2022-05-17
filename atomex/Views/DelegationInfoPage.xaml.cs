using atomex.ViewModel;
using Atomex.Blockchain.Tezos;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationInfoPage : ContentPage
    {
        public DelegationInfoPage(Delegation delegationViewModel)
        {
            InitializeComponent();
            BindingContext = delegationViewModel;
        }
    }
}
