using System;
using Xamarin.Forms;

namespace atomex
{
    public partial class SwapInfoPage : ContentPage
    {
        public SwapInfoPage(SwapViewModel swapViewModel)
        {
            InitializeComponent();
            BindingContext = swapViewModel;
            SetVisualState("SwapProgress");
        }

        private void OnButtonClicked(object sender, EventArgs args)
        {
            string state = ((Button)sender).CommandParameter.ToString();
            SetVisualState(state);
        }

        private void SetVisualState(string state)
        {
            VisualStateManager.GoToState(ProgressButton, state);
            VisualStateManager.GoToState(DetailsButton, state);
            VisualStateManager.GoToState(ProgressTab, state);
            VisualStateManager.GoToState(DetailsTab, state);
        }
    }
}
