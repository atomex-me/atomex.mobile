using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class AddressesPage : ContentPage
    {
        public AddressesPage(AddressesViewModel addressesViewModel)
        {
            InitializeComponent();
            BindingContext = addressesViewModel;
        }

        private void ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
                ((ListView)sender).SelectedItem = null;
        }
    }
}
