using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;
using ZXing.Client.Result;

namespace atomex.Views.SettingsOptions.Dapps
{
    public partial class ConfirmDappPage : ContentPage
    {
        public ConfirmDappPage()
        {
            InitializeComponent();
        }

        public ConfirmDappPage(ConfirmDappViewModel dappViewModel)
        {
            InitializeComponent();
            BindingContext = dappViewModel;
        }
    }
}