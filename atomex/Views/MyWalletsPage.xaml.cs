using atomex.Common;
using Xamarin.Forms;

namespace atomex
{
    public partial class MyWalletsPage : ContentPage
    {

        private MyWalletsViewModel _myWalletsViewModel;

        public MyWalletsPage()
        {
            InitializeComponent();
        }

        public MyWalletsPage(MyWalletsViewModel myWalletsViewModel)
        {
            InitializeComponent();
            _myWalletsViewModel = myWalletsViewModel;
            BindingContext = myWalletsViewModel;
        }

        private async void OnWalletTapped(object sender, ItemTappedEventArgs args)
        {
            if (args.Item != null)
            {
                await Navigation.PushAsync(new UnlockWalletPage(new UnlockViewModel(_myWalletsViewModel.AtomexApp, args.Item as WalletInfo)));
                var listView = sender as ListView;
                if (listView != null)
                    listView.SelectedItem = null;
            }
        }
    }
}
