using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class OutputsListPage : ContentPage
    {
        public OutputsListPage()
        {
            InitializeComponent();
        }

        public OutputsListPage(BitcoinBasedSendViewModel bitcoinBasedSendViewModel)
        {
            InitializeComponent();
            BindingContext = bitcoinBasedSendViewModel;
        }
    }
}
