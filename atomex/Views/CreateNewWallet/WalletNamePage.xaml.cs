using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
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

        private async void EntryFocused(object sender, FocusEventArgs args)
        {
            Frame.HasShadow = args.IsFocused;
            Error.IsVisible = false;
            
            if (args.IsFocused)
                await Content.TranslateTo(0, -Page.Height / 2 + Frame.Height + Labels.Height, 500, Easing.CubicInOut);
            else
                await Content.TranslateTo(0, 0, 1000, Easing.BounceOut);
        }

        private void OnEntryTapped(object sender, EventArgs args)
        {
            Entry.Focus();
        }

        private async void OnTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHint.IsVisible)
                {
                    NameHint.IsVisible = true;
                    NameHint.Text = Entry.Placeholder;

                    _ = NameHint.FadeTo(1, 500, Easing.Linear);
                    _ = Entry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = NameHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    NameHint.FadeTo(0, 500, Easing.Linear),
                    Entry.TranslateTo(0, 0, 500, Easing.CubicOut),
                    NameHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                
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
