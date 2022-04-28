using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class SearchOutputsPage : ContentPage
    {
        public SearchOutputsPage(SelectOutputsViewModel selectOutputsViewModel)
        {
            InitializeComponent();
            BindingContext = selectOutputsViewModel;
        }

        protected override void OnDisappearing()
        {
            var vm = (SelectOutputsViewModel)BindingContext;
            if (vm.BackCommand.CanExecute(null))
                vm.BackCommand.Execute(null);

            base.OnDisappearing();
        }
    }
}
