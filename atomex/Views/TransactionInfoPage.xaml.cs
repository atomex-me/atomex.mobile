using System;
using atomex.Resources;
using atomex.ViewModel.TransactionViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class TransactionInfoPage : ContentPage
    {
        private TransactionViewModel _transactionViewModel;

        private CurrencyViewModel _currencyViewModel;

        public TransactionInfoPage(TransactionViewModel transactionViewModel, CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            if (transactionViewModel != null)
            {
                _transactionViewModel = transactionViewModel;
                BindingContext = transactionViewModel;
            }
            _currencyViewModel = currencyViewModel;
        }
    }
}
