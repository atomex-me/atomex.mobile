using System;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class SettingsPeriodOfInactiveListOptionsPage : ContentPage
    {
        public Action<int> OnOptionSelected;

        public SettingsPeriodOfInactiveListOptionsPage(SettingsViewModel SettingsViewModel, Action<int> onOptionSelected)
        {
            InitializeComponent();
            OnOptionSelected = onOptionSelected;
            BindingContext = SettingsViewModel;
        }

        async void OnOptionTapped(object sender, EventArgs args)
        {
            var viewCell = sender as ViewCell;

            if (viewCell == Option1)
                OnOptionSelected?.Invoke(5);
            if (viewCell == Option2)
                OnOptionSelected?.Invoke(10);
            if (viewCell == Option3)
                OnOptionSelected?.Invoke(30);
            if (viewCell == Option4)
                OnOptionSelected?.Invoke(60);
            if (viewCell == Option5)
                OnOptionSelected?.Invoke(90);
            if (viewCell == Option6)
                OnOptionSelected?.Invoke(180);

            await Navigation.PopAsync();
        }
    }
}
