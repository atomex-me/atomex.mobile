using System;
using Xamarin.Forms;

namespace atomex
{
    public partial class WalletNamePage : ContentPage
    {
        private CreateNewWalletViewModel _createNewWalletViewModel;

        public WalletNamePage()
        {
            InitializeComponent();
        }

        public WalletNamePage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void EntryFocused(object sender, FocusEventArgs e)
        {
            Frame.HasShadow = true;
            Error.IsVisible = false;
        }
        private void EntryUnfocused(object sender, FocusEventArgs e)
        {
            Frame.HasShadow = false;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHint.IsVisible)
                {
                    NameHint.IsVisible = true;
                    NameHint.Text = Entry.Placeholder;
                }
            }
            else
            {
                NameHint.IsVisible = false;
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            var result = _createNewWalletViewModel.SaveWalletName();
            if (result == null)
            {
                if (_createNewWalletViewModel.CurrentAction == CreateNewWalletViewModel.Action.Create)
                {
                    await Navigation.PushAsync(new CreateMnemonicPage(_createNewWalletViewModel));
                    return;
                }
                if (_createNewWalletViewModel.CurrentAction == CreateNewWalletViewModel.Action.Restore)
                {
                    await Navigation.PushAsync(new WriteMnemonicPage(_createNewWalletViewModel));
                    return;
                }
            }
            else
            {
                Error.Text = result;
                Error.IsVisible = true;
            }
        }
    }
}
