using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.SettingsOptions.Dapps
{
    public partial class DappsPage : ContentPage
    {
        public DappsPage(DappsViewModel dappsViewModel)
        {
            InitializeComponent();
            BindingContext = dappsViewModel;
        }
    }
}