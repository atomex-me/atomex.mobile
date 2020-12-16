using System;
using System.Globalization;
using atomex.Resources;
using atomex.ViewModel;
using Serilog;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegatePage : ContentPage
    {
        private DelegateViewModel _delegateViewModel;

        public DelegatePage()
        {
            InitializeComponent();
        }

        public DelegatePage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            _delegateViewModel = delegateViewModel;
            BindingContext = delegateViewModel;
            Fee.Text = delegateViewModel.Fee.ToString();
            if (delegateViewModel.BakerViewModel != null)
                ToAddressEntry.Text = delegateViewModel.Address;
        }

        public DelegatePage(DelegateViewModel delegateViewModel, string address)
        {
            InitializeComponent();
            _delegateViewModel = delegateViewModel;
            BindingContext = delegateViewModel;
            delegateViewModel.SetWalletAddress(address);
            Fee.Text = delegateViewModel.Fee.ToString();
            if (delegateViewModel.BakerViewModel != null)
                ToAddressEntry.Text = delegateViewModel.Address;
        }

        private async void OnBakerPickerClicked(object sender, EventArgs args)
        {
            var bakersListPage = new BakerListPage(_delegateViewModel, selected =>
            {
                _delegateViewModel.BakerViewModel = selected;
                ToAddressEntry.Text = selected.Address;
            });
            await Navigation.PushAsync(bakersListPage);
        }

        private void OnFromAddressPickerFocused(object sender, FocusEventArgs args)
        {
            //FromAddressFrame.HasShadow = args.IsFocused;
        }

        private void OnFromAddressPickerClicked(object sender, EventArgs args)
        {
            AddressPicker.Focus();
        }

        private void OnToAddressEntryFocused(object sender, FocusEventArgs args)
        {
            //ToAddressFrame.HasShadow = args.IsFocused;
            if (!args.IsFocused)
            {
                _delegateViewModel.Address = ToAddressEntry.Text;
            }
        }

        private void OnFeeEntryFocused(object sender, FocusEventArgs args)
        {
            //FeeFrame.HasShadow = args.IsFocused;

            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, FeeFrame.Height, true);
                    return false;
                });
            }
            else
            {
                decimal fee;
                try
                {
                    decimal.TryParse(Fee.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out fee);
                }
                catch (FormatException)
                {
                    fee = 0;
                }
                _delegateViewModel.Fee = fee;
                Fee.Text = _delegateViewModel.Fee.ToString();

                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnFeeEntryTapped(object sender, EventArgs args)
        {
            Fee.Focus();
        }

        private void OnUseDefaultFeeToggled(object sender, ToggledEventArgs args)
        {
            if (args.Value)
            {
                if (!string.IsNullOrEmpty(Fee.Text))
                    Fee.Text = _delegateViewModel.Fee.ToString();
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            try
            {
                BlockActions(true);
                var error = await _delegateViewModel.Validate();
                BlockActions(false);
                if (error != null)
                {
                    await DisplayAlert(AppResources.Error, error, AppResources.AcceptButton);
                    return;
                }
                if (_delegateViewModel.BakerViewModel.IsFull)
                {
                    var res = await DisplayAlert(AppResources.Warning, AppResources.BakerIsOverdelegatedWarning, AppResources.AcceptButton, AppResources.CancelButton);
                    if (!res) return;
                }
                if (_delegateViewModel.BakerViewModel.MinDelegation > _delegateViewModel.WalletAddressViewModel.AvailableBalance)
                {
                    var res = await DisplayAlert(AppResources.Warning, AppResources.DelegationLimitWarning, AppResources.AcceptButton, AppResources.CancelButton);
                    if (!res) return;
                }
                await Navigation.PushAsync(new DelegationConfirmationPage(_delegateViewModel));
            }
            catch (Exception e)
            {
                BlockActions(false);
                await DisplayAlert(AppResources.Error, AppResources.DelegationValidationError, AppResources.AcceptButton);
                Log.Error(e, "Delegation validation error");
            }
        }

        private void BlockActions(bool flag)
        {
            ValidatingLoader.IsVisible = ValidatingLoader.IsRunning = flag;
            NextButton.IsEnabled = !flag;
            if (flag)
            {
                NextButton.Text = "";
                Content.Opacity = 0.5;
            }
            else
            {
                NextButton.Text = AppResources.NextButton;
                Content.Opacity = 1;
            }
        }
    }
}
