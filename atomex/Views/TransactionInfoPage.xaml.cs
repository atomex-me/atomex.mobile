using System;
using atomex.ViewModel.TransactionViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
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

        private async void OnCopyButtonClicked(object sender, EventArgs args)
        {
            if (_transactionViewModel != null)
            {
                await Clipboard.SetTextAsync(_transactionViewModel.Id);
                await DisplayAlert("Transaction ID copied", _transactionViewModel.Id, "Ok");
            }
            else
            {
                await DisplayAlert("Error", "Copy error", "Ok");
            }
        }

        private void OnShowInExplorerClicked(object sender, EventArgs args)
        {
            if (_transactionViewModel != null)
            {
                Launcher.OpenAsync(new Uri(_transactionViewModel.TxExplorerUri));
            }
        }
    }
}
