using atomex.ViewModels.TransactionViewModels;
using Xamarin.Forms;

namespace atomex.Views
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