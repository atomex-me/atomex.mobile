using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views.Delegate
{
    public partial class SearchBakerPage : ContentPage
    {
        public SearchBakerPage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
        }

        protected override void OnDisappearing()
        {
            var vm = (DelegateViewModel)BindingContext;
            if (vm.ClearSearchFieldCommand.CanExecute(null))
                vm.ClearSearchFieldCommand.Execute(null);

            base.OnDisappearing();
        }
    }
}
