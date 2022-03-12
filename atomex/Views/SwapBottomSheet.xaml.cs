using System;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class SwapBottomSheet : PopupPage
    {
        public SwapBottomSheet(SwapViewModel swapViewModel)
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

        public void OnClose()
        {
            if (BindingContext is SwapViewModel)
            {
                var swapViewModel = (SwapViewModel)BindingContext;
                if (swapViewModel.CloseBottomSheetCommand.CanExecute(null))
                    swapViewModel.CloseBottomSheetCommand.Execute(null);
            }
        }
    }
}
