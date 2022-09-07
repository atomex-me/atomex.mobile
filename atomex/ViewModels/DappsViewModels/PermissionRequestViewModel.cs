using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using atomex.Common;
using atomex.Resources;
using Beacon.Sdk.Beacon.Permission;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace atomex.ViewModels.DappsViewModels
{
    public class PermissionRequestViewModel : BaseViewModel
    {
        public string DappName { get; set; }
        public string DappLogo { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public List<PermissionScope> Permissions { get; set; }
        public List<string> PermissionStrings => BeaconHelper.GetPermissionStrings(Permissions);

        public string SubTitle => string.Format(AppResources.DappWantsToConnect, DappName);

        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public PermissionRequestViewModel()
        {
            OnAllowCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);
        }

        public Func<Task> OnAllow { get; set; }
        public Func<Task> OnReject { get; set; }

        private ReactiveCommand<Unit, Unit> _onAllowCommand;

        public ReactiveCommand<Unit, Unit> OnAllowCommand =>
            _onAllowCommand ??= ReactiveCommand.CreateFromTask(async () => await OnAllow());

        private ReactiveCommand<Unit, Unit> _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());
    }
}