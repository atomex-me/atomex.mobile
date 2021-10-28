using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;
using ZXing.Client.Result;

namespace atomex.Views.SettingsOptions.Dapps
{
    public partial class DappsPage : ContentPage
    {
        public DappsPage()
        {
            InitializeComponent();
        }

        public DappsPage(DappsViewModel dappsViewModel)
        {
            InitializeComponent();
            BindingContext = dappsViewModel;
        }
    }
}