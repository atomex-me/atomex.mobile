using System;
using System.Collections.Generic;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class BakerListPage : ContentPage
    {
        public Action<BakerViewModel> OnBakerSelected;

        public BakerListPage()
        {
            InitializeComponent();
        }

        public BakerListPage(DelegateViewModel delegateViewModel, Action<BakerViewModel> onBakerSelected)
        {
            InitializeComponent();
            BindingContext = delegateViewModel;
            OnBakerSelected = onBakerSelected;
        }

        private async void OnBakerTapped(object sender, ItemTappedEventArgs args)
        {
            if (args.Item == null)
                return;

            var baker = args.Item as BakerViewModel;

            if (baker == null)
                return;

            OnBakerSelected.Invoke(baker);

            await Navigation.PopAsync();
        }
    }
}
