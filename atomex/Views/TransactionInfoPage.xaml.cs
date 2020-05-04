using System;
using atomex.Resources;
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
                await DisplayAlert(AppResources.TransactionIdCopied, _transactionViewModel.Id, AppResources.AcceptButton);
            }
            else
            {
                await DisplayAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
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
