using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using atomex.ViewModels.DappsViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DappInfoPage : ContentPage
    {
        public DappInfoPage(DappViewModel dapp)
        {
            InitializeComponent();
            BindingContext = dapp;
        }
    }
}