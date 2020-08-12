using System;
using atomex.Models;
using atomex.ViewModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationInfoPage : ContentPage
    {
        private DelegateViewModel _delegateViewModel;

        private Delegation _delegationModel;

        public DelegationInfoPage(DelegateViewModel delegateViewModel, Delegation delegationModel)
        {
            InitializeComponent();
            _delegateViewModel = delegateViewModel;
            _delegationModel = delegationModel;
            BindingContext = delegationModel;
        }

        private void OnCheckRewardsButtonClicked(object sender, EventArgs args)
        {
            if (_delegationModel != null && !string.IsNullOrEmpty(_delegationModel.BbUri))
            {
                Launcher.OpenAsync(new Uri(_delegationModel.BbUri + _delegationModel.Address));
            }
        }
        private async void OnChangeBakerButtonClicked(object sender, EventArgs args)
        {
            _delegateViewModel.SetWalletAddress(_delegationModel.Address);
            await Navigation.PushAsync(new DelegatePage(_delegateViewModel, _delegationModel.Address));
        }
    }
}
