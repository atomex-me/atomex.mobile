using System;
using atomex.ViewModels.DappsViewModels;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OperationRequestPopup : PopupPage
    {
        public OperationRequestPopup(OperationRequestViewModel operationRequestViewModel)
        {
            InitializeComponent();
            BindingContext = operationRequestViewModel;
        }
        
        protected override bool OnBackgroundClicked()
        {
            if (BindingContext is OperationRequestViewModel)
            {
                var vm = (OperationRequestViewModel)BindingContext;
                vm?.OnRejectCommand.Execute();
            }
            
            return true;
        }   

        protected override void OnDisappearing()
        {
            if (BindingContext is IDisposable vm)
                vm.Dispose();
        }
    }
}