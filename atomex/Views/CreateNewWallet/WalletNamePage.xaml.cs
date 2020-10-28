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

        private void EntryFocused(object sender, FocusEventArgs args)
        {
            Frame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
            {
                if(args.IsFocused)
                    ScrollView.ScrollToAsync(0, ScrollView.Height/2 - (Frame.Height + Labels.Height), true);
                else
                    ScrollView.ScrollToAsync(0, 0, true);
                return false;
            });
        }

        private async void OnTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHint.IsVisible)
                {
                    NameHint.IsVisible = true;
                    NameHint.Text = Entry.Placeholder;

                    await Task.Run(() =>
                    {
                        NameHint.FadeTo(1, 500, Easing.Linear);
                    });
                    await Task.Run(() =>
                    {
                        Entry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    });
                    await Task.Run(() =>
                    {
                        NameHint.TranslateTo(0, -20, 500, Easing.CubicOut);
                    });
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    NameHint.FadeTo(0, 500, Easing.Linear);
                });
                await Task.Run(() =>
                {
                    Entry.TranslateTo(0, 0, 500, Easing.CubicOut);
                });
                await NameHint.TranslateTo(0, -10, 500, Easing.CubicOut);
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
