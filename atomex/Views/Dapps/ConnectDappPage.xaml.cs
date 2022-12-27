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
        private void DisableScanner(object sender, EventArgs e)
        {
            SwitchScanner(false);
        }
        private void EnableScanner(object sender, EventArgs e)
        {
            SwitchScanner(true);
        }

        private void SwitchScanner(bool flag)
        {
            Scanner.IsScanning = flag;
            Scanner.IsAnalyzing = flag;
            Overlay.IsVisible = flag;
        }

        protected override void OnDisappearing()
        {
            SwitchScanner(false); 
            
            if (BindingContext is ConnectDappViewModel)
            {
                var vm = (ConnectDappViewModel)BindingContext;
                vm?.Reset();
            }
        }
    }
}