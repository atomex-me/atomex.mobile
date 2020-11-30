using System;
using atomex.ViewModel;
using Xamarin.Forms;
using System.Linq;
using Serilog;

namespace atomex
{
    public partial class BakerListPage : ContentPage
    {
        private DelegateViewModel _delegateViewModel;

        public Action<BakerViewModel> OnBakerSelected;

        public BakerListPage()
        {
            InitializeComponent();
        }

        public BakerListPage(DelegateViewModel delegateViewModel, Action<BakerViewModel> onBakerSelected)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
            _delegateViewModel = delegateViewModel;
            OnBakerSelected = onBakerSelected;
        }

        private async void BakerSelected(object sender, SelectionChangedEventArgs args)
        {
            if (args.CurrentSelection.Count > 0)
            {
                var baker = args.CurrentSelection.First() as BakerViewModel;

                if (baker == null)
                    return;

                OnBakerSelected.Invoke(baker);

                await Navigation.PopAsync();
            }
        }

        private void OnSearchBakerTextChanged(object sender, TextChangedEventArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(args.NewTextValue))
                    BakersListView.ItemsSource = _delegateViewModel.FromBakersList;
                else
                    BakersListView.ItemsSource = _delegateViewModel.FromBakersList.Where(x => x.Name.ToLower().Contains(args.NewTextValue.ToLower()));
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }
    }
}
