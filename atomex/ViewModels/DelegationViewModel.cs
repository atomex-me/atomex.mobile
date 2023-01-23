using System;
using System.Reactive;
using System.Windows.Input;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using atomex.Resources;
using ReactiveUI;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace atomex.ViewModels
{
    public class DelegationViewModel
    {
        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public string ExplorerUri { get; set; }
        public DateTime DelegationTime { get; set; }
        public DateTime LocalTime => DelegationTime.ToLocalTime();
        public string LocalTimeString => LocalTime.ToString(AppResources.Culture.DateTimeFormat.FullDateTimePattern, AppResources.Culture);
        public DelegationStatus Status { get; set; }
        public string StatusString => Status.GetDescription();

        public Action<DelegationViewModel> ShowActionSheet { get; set; }
        public Action CloseActionSheet { get; set; }
        public Action<string> CopyAddress { get; set; }
        public Action<DelegationViewModel> ChangeBaker { get; set; }
        public Action<DelegationViewModel> Undelegate { get; set; }

        public DelegationViewModel()
        {
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

        private ReactiveCommand<Unit, Unit> _undelegateCommand;

        public ReactiveCommand<Unit, Unit> UndelegateCommand => _undelegateCommand ??=
            ReactiveCommand.Create(() => Undelegate?.Invoke(this));

        private ICommand _closeBottomSheetCommand;

        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??=
            new Command(() => CloseActionSheet?.Invoke());

        private ReactiveCommand<Unit, Unit> _delegationActionSheetCommand;

        public ReactiveCommand<Unit, Unit> DelegationActionSheetCommand => _delegationActionSheetCommand ??=
            ReactiveCommand.Create(() => ShowActionSheet?.Invoke(this));
    }
}