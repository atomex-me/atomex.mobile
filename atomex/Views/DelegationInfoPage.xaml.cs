using System;
using atomex.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegationInfoPage : ContentPage
    {
        private Delegation _delegationModel;

        public DelegationInfoPage(Delegation delegationModel)
        {
            InitializeComponent();
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
    }
}
