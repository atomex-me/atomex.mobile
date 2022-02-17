using atomex.ViewModel.SendViewModels;
using Xamarin.Forms;

namespace atomex.Views.Send
{
    public partial class SearchAddressPage : ContentPage
    {
        public SearchAddressPage(SelectAddressViewModel selectAddressViewModel)
        {
            InitializeComponent();
            BindingContext = selectAddressViewModel;
        }

        protected override void OnDisappearing()
        {
            var vm = (SelectAddressViewModel)BindingContext;
            if (vm.BackCommand.CanExecute(null))
                vm.BackCommand.Execute(null);

            base.OnDisappearing();
        }

    }
}
