using System;
using atomex.Resources;
using atomex.ViewModel;
using Serilog;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationConfirmationPage : ContentPage
    {
        private DelegateViewModel _delegateViewModel;

        private const int BACK_COUNT = 3;

        public DelegationConfirmationPage()
        {
            InitializeComponent();
        }

        public DelegationConfirmationPage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            _delegateViewModel = delegateViewModel;
            BindingContext = delegateViewModel;
        }

        async void OnDelegateButtonClicked(object sender, EventArgs args)
        {
            try
            {
                BlockActions(true);
                var result = await _delegateViewModel.Delegate();
                if (result.Error != null)
                {
                    BlockActions(false);
                    await DisplayAlert(AppResources.Error, result.Error.Description, AppResources.AcceptButton);
                    return;
                }
                await _delegateViewModel.LoadDelegationInfoAsync();
                var res = await DisplayAlert(AppResources.SuccessDelegation, AppResources.DelegationListWillBeUpdated + "\r\n" + AppResources.ExplorerUri + ": " + result.Value, null, AppResources.CopyAndExitButton);
                if (!res)
                {
                    await Clipboard.SetTextAsync(result.Value);
                    for (var i = 1; i < BACK_COUNT; i++)
                    {
                        Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                    }
                    await Navigation.PopAsync();
                }
            }
            catch (Exception e)
            {
                BlockActions(false);
                await DisplayAlert(AppResources.Error, AppResources.DelegationError, AppResources.AcceptButton);
                Log.Error(e, "Delegation error");
            }
        }

        private void BlockActions(bool flag)
        {
            SendingLoader.IsVisible = SendingLoader.IsRunning = flag;
            DelegateButton.IsEnabled = !flag;
            if (flag)
            {
                Content.Opacity = 0.5;
            }
            else
            {
                Content.Opacity = 1;
            }
        }
    }
}
