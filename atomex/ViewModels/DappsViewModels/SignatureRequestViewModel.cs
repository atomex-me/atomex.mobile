using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using atomex.Common;

namespace atomex.ViewModels.DappsViewModels
{
    public class SignatureRequestViewModel : BaseViewModel
    {
        public string DappName { get; set; }
        public string DappLogo { get; set; }
        public string Payload { get; set; }
        public Func<Task> OnSign { get; set; }
        public Func<Task> OnReject { get; set; }

        [ObservableAsProperty] public bool IsSigning { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public SignatureRequestViewModel()
        {
            OnSignCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSigning);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);
        }

        private ReactiveCommand<Unit, Unit> _onSignCommand;

        public ReactiveCommand<Unit, Unit> OnSignCommand =>
            _onSignCommand ??= ReactiveCommand.CreateFromTask(async () => await OnSign());

        private ReactiveCommand<Unit, Unit> _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());
    }
}