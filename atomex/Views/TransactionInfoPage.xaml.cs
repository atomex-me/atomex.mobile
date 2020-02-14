using System;
using atomex.ViewModel.TransactionViewModels;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class TransactionInfoPage : ContentPage
    {
        private TransactionViewModel _transactionViewModel;
        public TransactionInfoPage(TransactionViewModel transactionViewModel)
        {
            InitializeComponent();
            if (transactionViewModel != null)
            {
                _transactionViewModel = transactionViewModel;
                BindingContext = transactionViewModel;
            }
        }

        private void OnShowInExplorerClicked(object sender, EventArgs args)
        {
            if (_transactionViewModel != null)
            {
                Device.OpenUri(new Uri(_transactionViewModel.TxExplorerUri));
            }
        }
    }
}
