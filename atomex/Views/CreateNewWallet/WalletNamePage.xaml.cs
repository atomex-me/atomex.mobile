using System;
using System.Threading.Tasks;
using atomex.ViewModels;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class WalletNamePage : ContentPage
    {
        public WalletNamePage()
        {
            InitializeComponent();
        }

        public WalletNamePage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            BindingContext = createNewWalletViewModel;
        }

        private void EntryFocused(object sender, FocusEventArgs args)
        {
            Frame.HasShadow = args.IsFocused;

            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, Entry.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
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

        protected override void OnDisappearing()
        {
            var vm = (CreateNewWalletViewModel)BindingContext;
            if (vm.ClearWarningCommand.CanExecute(null))
            {
                vm.ClearWarningCommand.Execute(null);
            }
            base.OnDisappearing();
        }
    }
}
