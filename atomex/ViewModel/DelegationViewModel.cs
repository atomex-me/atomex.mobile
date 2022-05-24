using System;
using System.Reactive;
using atomex.Resources;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using ReactiveUI;
using Xamarin.Essentials;

namespace atomex.ViewModel
{
    public class DelegationViewModel
    {
        private INavigationService _navigationService { get; set; }

        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public string ExplorerUri { get; set; }
        public DateTime DelegationTime { get; set; }
        public DelegationStatus Status { get; set; }
        public string StatusString => Status.GetDescription();

        public Action<string> CopyAddress { get; set; }
        public Action<DelegationViewModel> ChangeBaker { get; set; }
        public Action<DelegationViewModel> Undelegate { get; set; }

        public DelegationViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(_navigationService));
        }
   
        private ReactiveCommand<Unit, Unit> _checkRewardsCommand;
        public ReactiveCommand<Unit, Unit> CheckRewardsCommand => _checkRewardsCommand ??=
            ReactiveCommand.CreateFromTask(() => Launcher.OpenAsync(new Uri(ExplorerUri + Address)));

        private ReactiveCommand<string, Unit> _copyAddressCommand;
        public ReactiveCommand<string, Unit> CopyAddressCommand => _copyAddressCommand ??=
            ReactiveCommand.Create<string>((value) => CopyAddress?.Invoke(value));

        private ReactiveCommand<Unit, Unit> _changeBakerCommand;
        public ReactiveCommand<Unit, Unit> ChangeBakerCommand => _changeBakerCommand ??=
            ReactiveCommand.Create(() => ChangeBaker?.Invoke(this));


        private ReactiveCommand<Unit, Unit> _delegationActionSheetCommand;
        public ReactiveCommand<Unit, Unit> DelegationActionSheetCommand => _delegationActionSheetCommand ??= ReactiveCommand.CreateFromTask(async () =>
        {
            string delegateAction = Status == DelegationStatus.NotDelegated
                ? AppResources.DelegateButton
                : AppResources.UndelegateButton;

            string changeAction = Status == DelegationStatus.NotDelegated
                ? null
                : AppResources.ChangeBaker;


            string[] actions = new string[]
            {
                delegateAction,
                changeAction
            };

            string result = await _navigationService?.DisplayActionSheet(AppResources.CancelButton, actions);

            if (result == delegateAction)
            {
                if (Status == DelegationStatus.NotDelegated)
                    ChangeBaker?.Invoke(this);
                else
                    Undelegate?.Invoke(this);

                return;
            }
            if (result == changeAction)
            {
                ChangeBaker?.Invoke(this);
            }
        });
    }
}
