using System;
using atomex.Resources;
using atomex.ViewModel.TransactionViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using atomex.Services;
using atomex.ViewModel;

namespace atomex
{
    public partial class TransactionInfoPage : ContentPage
    {
        private TransactionViewModel _transactionViewModel;

        private CurrencyViewModel _currencyViewModel;

        private IToastService _toastService;

        public TransactionInfoPage(TransactionViewModel transactionViewModel, CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            if (transactionViewModel != null)
            {
                _transactionViewModel = transactionViewModel;
                BindingContext = transactionViewModel;
            }
            _currencyViewModel = currencyViewModel;
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

        private async void OnDeleteTxButtonClicked(object sender, EventArgs args)
        {
            if (_currencyViewModel != null)
            {
                var res = await DisplayAlert(AppResources.Warning, AppResources.RemoveTxWarning, AppResources.AcceptButton, AppResources.CancelButton);
                if (!res) return;
                _currencyViewModel.RemoveTransactonAsync(_transactionViewModel.Id);
                await Navigation.PopAsync();
            }
        }
    }
}
