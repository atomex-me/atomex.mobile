using System;
using atomex.ViewModels.DappsViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectDappPage : ContentPage
    {
        public ConnectDappPage(ConnectDappViewModel connectDappViewModel)
        {
            InitializeComponent();
            BindingContext = connectDappViewModel;
        }
        private void DisableScannerOverlay(object sender, EventArgs e)
        {
            Overlay.IsVisible = false;
        }
        private void EnableScannerOverlay(object sender, EventArgs e)
        {
            Overlay.IsVisible = true;
        }
    }
}