using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;
using atomex.Common;
using ReactiveUI;
using Hex = Atomex.Common.Hex;

namespace atomex.ViewModels.DappsViewModels
{
    public class SignatureRequestViewModel : BaseViewModel
    {
        private const int _payloadServiceBytesNum = 6;
        public string DappName { get; set; }
        public string DappLogo { get; set; }
        [Reactive] public string BytesPayload { get; set; }
        [ObservableAsProperty] public string RawPayload { get; }
        [Reactive] public bool IsRawTab { get; set; }
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

            this.WhenAnyValue(vm => vm.BytesPayload)
                .WhereNotNull()
                .Select(bytesPayload =>
                {
                    try
                    {
                        var parsedBytes = Hex.FromString(bytesPayload[(_payloadServiceBytesNum * 2)..]);
                        return System.Text.Encoding.UTF8.GetString(parsedBytes);
                    }
                    catch (Exception)
                    {
                        return "Can't parse income payload to sign, check it out on bytes tab.";
                    }
                })
                .ToPropertyExInMainThread(this, vm => vm.RawPayload);

            IsRawTab = true;
        }

        private ReactiveCommand<Unit, Unit> _onSignCommand;

        public ReactiveCommand<Unit, Unit> OnSignCommand =>
            _onSignCommand ??= ReactiveCommand.CreateFromTask(async () => await OnSign());

        private ReactiveCommand<Unit, Unit> _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());

        private ReactiveCommand<bool, Unit> _changeTabCommand;

        public ReactiveCommand<bool, Unit> ChangeTabCommand =>
            _changeTabCommand ??= _changeTabCommand = ReactiveCommand.Create<bool>(value => { IsRawTab = value; });
    }
}