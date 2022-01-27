using System;
using System.Threading.Tasks;
using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views.Send
{
    public partial class ToAddressPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public ToAddressPage(SelectAddressViewModel selectAddressViewModel)
        {
            InitializeComponent();
            BindingContext = selectAddressViewModel;
            SetVisualState("External");

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);
            selectedItemBackgroundColor = (Color)selectedColor;
        }

        private async void OnItemTapped(object sender, EventArgs args)
        {
            Grid selectedItem = (Grid)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;
            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
            selectedItem.IsEnabled = true;
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
