using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atomex.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OperationRequestListPage : ContentPage
    {
        Color selectedItemBackgroundColor;

        public OperationRequestListPage(OperationRequestViewModel operationRequest)
        {
            InitializeComponent();
            BindingContext = operationRequest;

            string selectedColorName = "ListViewSelectedBackgroundColor";

            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                selectedColorName = "ListViewSelectedBackgroundColorDark";

            Application.Current.Resources.TryGetValue(selectedColorName, out var selectedColor);

            selectedItemBackgroundColor = (Color)selectedColor;
        }
        
        private async void OnItemTapped(object sender, EventArgs args)
        {
            StackLayout selectedItem = (StackLayout)sender;
            selectedItem.IsEnabled = false;
            Color initColor = selectedItem.BackgroundColor;
            selectedItem.BackgroundColor = selectedItemBackgroundColor;

            await Task.Delay(500);

            selectedItem.BackgroundColor = initColor;
        }
    }
}