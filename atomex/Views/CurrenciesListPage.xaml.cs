using Xamarin.Forms;
using atomex.ViewModel;
using Atomex;
using System;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        private INavigationService _navigationService { get; }

        private IAtomexApp AtomexApp { get; }

        public CurrenciesListPage()
        {
            InitializeComponent();
        }

        public CurrenciesListPage(CurrenciesViewModel currenciesViewModel, IAtomexApp atomexApp, INavigationService navigationService)
        {
            InitializeComponent();
            AtomexApp = atomexApp ?? throw new ArgumentNullException(nameof(AtomexApp));
            _navigationService = navigationService;
            BindingContext = currenciesViewModel;
        }

        async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                await Navigation.PushAsync(new CurrencyPage(e.Item as CurrencyViewModel, AtomexApp, _navigationService));
                var listView = sender as ListView;
                if (listView != null)
                    listView.SelectedItem = null;
            }
        }
    }
}
