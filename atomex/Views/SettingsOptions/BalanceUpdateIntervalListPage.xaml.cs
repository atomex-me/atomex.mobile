using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.SettingsOptions
{
    public partial class BalanceUpdateIntervalListPage : ContentPage
    {
        public Action<int> OnOptionSelected;

        public BalanceUpdateIntervalListPage(SettingsViewModel settingsViewModel, Action<int> onOptionSelected)
        {
            InitializeComponent();
            OnOptionSelected = onOptionSelected;
            BindingContext = settingsViewModel;
        }

        async void OnOptionTapped(object sender, EventArgs args)
        {
            var viewCell = sender as ViewCell;

            if (viewCell == Option1)
                OnOptionSelected?.Invoke(30);
            if (viewCell == Option2)
                OnOptionSelected?.Invoke(60);
            if (viewCell == Option3)
                OnOptionSelected?.Invoke(120);
            if (viewCell == Option4)
                OnOptionSelected?.Invoke(240);
            if (viewCell == Option5)
                OnOptionSelected?.Invoke(360);
            if (viewCell == Option6)
                OnOptionSelected?.Invoke(600);

            await Navigation.PopAsync();
        }
    }
}
