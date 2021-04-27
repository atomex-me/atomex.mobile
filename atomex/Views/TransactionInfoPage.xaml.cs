using atomex.ViewModel.TransactionViewModels;
using Xamarin.Forms;

namespace atomex
{
    public partial class TransactionInfoPage : ContentPage
    {
        public TransactionInfoPage(TransactionViewModel transactionViewModel)
        {
            InitializeComponent();
            BindingContext = transactionViewModel;
        }
    }
}
