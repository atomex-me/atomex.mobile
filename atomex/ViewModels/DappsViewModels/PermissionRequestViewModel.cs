using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Atomex;
using atomex.Common;
using atomex.Resources;
using Atomex.ViewModels;
using Beacon.Sdk.Beacon.Permission;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace atomex.ViewModels.DappsViewModels
{
    public class PermissionRequestViewModel : BaseViewModel
    {
        public string DappName { get; set; }
        public string DappLogo { get; set; }
        [Reactive] public string Address { get; set; }
        [Reactive] public decimal Balance { get; set; }
        public List<PermissionScope> Permissions { get; set; }
        public List<string> PermissionStrings => BeaconHelper.GetPermissionStrings(Permissions);

        public string SubTitle => string.Format(AppResources.DappWantsToConnect, DappName);

        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }
        
        public SelectAddressViewModel SelectAddressViewModel { get; set; }
        
        private readonly IAtomexApp _app;
        private INavigationService _navigationService;
        
        private TezosConfig _tezos { get; }

        public PermissionRequestViewModel(
            IAtomexApp app,
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            _tezos = (TezosConfig) _app.Account.Currencies.GetByName(TezosConfig.Xtz);
            
            OnAllowCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);
            
            SelectAddressViewModel =
                new SelectAddressViewModel(
                    account: _app.Account,
                    currency: _tezos,
                    navigationService: _navigationService,
                    mode: SelectAddressMode.Connect)
                {
                    ConfirmAction = (selectAddressViewModel, walletAddressViewModel) =>
                    {
                        Address = walletAddressViewModel?.Address;
                        Balance = walletAddressViewModel?.Balance ?? 0m;

                        _navigationService?.ClosePopup();
                    }
                };

            Address = SelectAddressViewModel.SelectedAddress?.Address;
            Balance = SelectAddressViewModel.SelectedAddress?.Balance ?? 0m;
        }

        public Func<WalletAddressViewModel, Task> OnAllow { get; set; }
        public Func<Task> OnReject { get; set; }
        public Func<SelectAddressViewModel, Task> OnChangeAddress { get; set; }

        private ReactiveCommand<Unit, Unit> _onAllowCommand;

        public ReactiveCommand<Unit, Unit> OnAllowCommand =>
            _onAllowCommand ??= ReactiveCommand.CreateFromTask(async () => await OnAllow(SelectAddressViewModel.SelectedAddress));

        private ReactiveCommand<Unit, Unit> _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());
        
        private ReactiveCommand<Unit, Unit> _onChangeAddressCommand;

        public ReactiveCommand<Unit, Unit> OnChangeAddressCommand =>
            _onChangeAddressCommand ??=
                ReactiveCommand.CreateFromTask(async () => await OnChangeAddress(SelectAddressViewModel));
    }
}