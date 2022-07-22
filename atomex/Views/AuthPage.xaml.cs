using System;
using atomex.ViewModels;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class AuthPage : ContentPage
    {
        public AuthPage()
        {
            InitializeComponent();
        }

        public AuthPage(UnlockViewModel unlockViewModel)
        {
            InitializeComponent();
            BindingContext = unlockViewModel;
        }

        public AuthPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            BindingContext = createNewWalletViewModel;
        }

        public AuthPage(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            BindingContext = settingsViewModel;
        }

        private async void OnItemTapped(object sender, EventArgs args)
        {
            Frame selectedItem = (Frame)sender;
            await selectedItem.FadeTo(0.6, 50, Easing.SinInOut);
            await selectedItem.FadeTo(1, 450, Easing.SinInOut);
        }

        protected override void OnDisappearing()
        {
            if (BindingContext is UnlockViewModel)
            {
                var vm = (UnlockViewModel)BindingContext;
                if (vm.BackCommand.CanExecute(null))
                {
                    vm.BackCommand.Execute(null);
                }
            }
            else if (BindingContext is SettingsViewModel)
            {
                var vm = (SettingsViewModel)BindingContext;
                if (vm.BackCommand.CanExecute(null))
                {
                    vm.BackCommand.Execute(null);
                }
            }

            base.OnDisappearing();
        }
    }
}
