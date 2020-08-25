using System;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel;
using Atomex.Wallet;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.Views.CreateNewWallet
{
    public partial class CreateStoragePasswordPage : ContentPage
    {

        private CreateNewWalletViewModel _createNewWalletViewModel;

        public CreateStoragePasswordPage()
        {
            InitializeComponent();
        }

        public CreateStoragePasswordPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            _createNewWalletViewModel = createNewWalletViewModel;
            BindingContext = createNewWalletViewModel;
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
            {
                if (args.IsFocused)
                    ScrollView.ScrollToAsync(0, ScrollView.Height / 2 - (PasswordFrame.Height + Labels.Height), true);
                else
                    ScrollView.ScrollToAsync(0, 0, true);
                return false;
            });
        }

        private void OnPasswordTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordHint.IsVisible)
                {
                    PasswordHint.IsVisible = true;
                    PasswordHint.Text = PasswordEntry.Placeholder;
                    PasswordEntry.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                PasswordEntry.VerticalTextAlignment = TextAlignment.Center;
                PasswordHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.StoragePassword, args.NewTextValue);
        }

        private void PasswordConfirmationEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordConfirmationFrame.HasShadow = args.IsFocused;
            Error.IsVisible = false;

            Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
            {
                if (args.IsFocused)
                    ScrollView.ScrollToAsync(0, ScrollView.Height / 2 - (PasswordFrame.Height + Labels.Height), true);
                else
                    ScrollView.ScrollToAsync(0, 0, true);
                return false;
            });
        }

        private void OnPasswordConfirmationTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordConfirmationHint.IsVisible)
                {
                    PasswordConfirmationHint.IsVisible = true;
                    PasswordConfirmationHint.Text = PasswordConfirmationEntry.Placeholder;
                    PasswordConfirmationEntry.VerticalTextAlignment = TextAlignment.Start;
                }
            }
            else
            {
                PasswordConfirmationEntry.VerticalTextAlignment = TextAlignment.Center;
                PasswordConfirmationHint.IsVisible = false;
            }
            _createNewWalletViewModel.SetPassword(CreateNewWalletViewModel.PasswordType.StoragePasswordConfirmation, args.NewTextValue);
        }

        private async void OnCreateButtonClicked(object sender, EventArgs args)
        {
            var result = _createNewWalletViewModel.CheckStoragePassword();
            if (result == null)
            {
                Content.Opacity = 0.3f;
                Loader.IsRunning = true;

                Account account = null;

                await Task.Run(async () =>
                {
                    account = await _createNewWalletViewModel.ConnectToWallet();
                });

                if (account != null)
                {
                    try
                    {
                        await SecureStorage.SetAsync(_createNewWalletViewModel.WalletName, PasswordEntry.Text);
                    }
                    catch (Exception ex)
                    {
                        // Possible that device doesn't support secure storage on device.
                    }

                    MainViewModel mainViewModel = null;

                    await Task.Run(() =>
                    {
                        mainViewModel = new MainViewModel(
                            _createNewWalletViewModel.AtomexApp,
                            account,
                            _createNewWalletViewModel.CurrentAction == CreateNewWalletViewModel.Action.Restore ? true : false);
                    });

                    Application.Current.MainPage = new MainPage(mainViewModel);

                    Content.Opacity = 1f;
                    Loader.IsRunning = false;
                }
                else
                {
                    Content.Opacity = 1f;
                    Loader.IsRunning = false;
                    await DisplayAlert(AppResources.Error, AppResources.CreateWalletError, AppResources.AcceptButton);
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
