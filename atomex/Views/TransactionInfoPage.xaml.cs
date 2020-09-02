using System;
using atomex.Resources;
using atomex.ViewModel.TransactionViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using atomex.Services;

namespace atomex
{
    public partial class TransactionInfoPage : ContentPage
    {
        private TransactionViewModel _transactionViewModel;

        private IToastService _toastService;

        public TransactionInfoPage(TransactionViewModel transactionViewModel)
        {
            InitializeComponent();
            if (transactionViewModel != null)
            {
                _transactionViewModel = transactionViewModel;
                BindingContext = transactionViewModel;
            }
            _toastService = DependencyService.Get<IToastService>();
        }

        private async void OnCopyButtonClicked(object sender, EventArgs args)
        {
            if (_transactionViewModel != null)
            {
                await Clipboard.SetTextAsync(_transactionViewModel.Id);
                _toastService?.Show(AppResources.TransactionIdCopied, ToastPosition.Bottom);
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
