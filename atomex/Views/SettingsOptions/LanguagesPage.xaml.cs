using System;
using atomex.Models;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.SettingsOptions
{
    public partial class LanguagesPage : ContentPage
    {
        public Action<Language> OnOptionSelected;

        public LanguagesPage(SettingsViewModel settingsViewModel, Action<Language> onOptionSelected)
        {
            InitializeComponent();
            OnOptionSelected = onOptionSelected;
            BindingContext = settingsViewModel;
        }

        public LanguagesPage(StartViewModel startViewModel, Action<Language> onOptionSelected)
        {
            InitializeComponent();
            OnOptionSelected = onOptionSelected;
            BindingContext = startViewModel;
        }

        async void OnLanguageTapped(object sender, EventArgs args)
        {
            Language language = (Language)((TappedEventArgs)args).Parameter;
            OnOptionSelected?.Invoke(language);

            await Navigation.PopAsync();
        }
    }
}
