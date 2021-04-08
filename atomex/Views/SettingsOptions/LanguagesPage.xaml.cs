using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.SettingsOptions
{
    public partial class LanguagesPage : ContentPage
    {

        public LanguagesPage(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            BindingContext = settingsViewModel;
        }

        public LanguagesPage(StartViewModel startViewModel)
        {
            InitializeComponent();
            BindingContext = startViewModel;
        }
    }
}
