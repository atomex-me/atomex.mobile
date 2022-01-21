using System;
using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views.Send
{
    public partial class ToAddressPage : ContentPage
    {
        public ToAddressPage(SendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
            SetVisualState("External");
        }

        private void OnButtonClicked(object sender, EventArgs args)
        {
            string state = ((Button)sender).CommandParameter.ToString();
            SetVisualState(state);
        }

        private void SetVisualState(string state)
        {
            VisualStateManager.GoToState(ExternalAddressButton, state);
            VisualStateManager.GoToState(MyAddressButton, state);
            VisualStateManager.GoToState(ExternalAddressUnderline, state);
            VisualStateManager.GoToState(MyAddressUnderline, state);
            VisualStateManager.GoToState(ExternalAddressTab, state);
            VisualStateManager.GoToState(MyAddressesTab, state);
        }
    }
}
