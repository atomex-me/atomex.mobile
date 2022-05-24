using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using atomex.Common;
using atomex.Views.Delegate;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Core;
using Atomex.Wallet;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public bool IsStakingAvailable { get; } = true;
        public bool HasCollectibles { get; } = true;
        public TezosConfig Tezos => Currency as TezosConfig;
        [Reactive] public ObservableCollection<DelegationViewModel> Delegations { get; set; }
        [Reactive] public DelegationViewModel SelectedDelegation { get; set; }
        private DelegateViewModel _delegateViewModel { get; set; }

        public TezosCurrencyViewModel(
             IAtomexApp app,
             CurrencyConfig currency,
             INavigationService navigationService)
            : base(app, currency, navigationService)
        {
            Delegations = new ObservableCollection<DelegationViewModel>();

            this.WhenAnyValue(vm => vm.SelectedDelegation)
               .WhereNotNull()
               .SubscribeInMainThread(d =>
               {
                   _navigationService?.SetInitiatedPage(TabNavigation.Portfolio);
                   if (d.Baker != null)
                       _navigationService?.ShowPage(new DelegationInfoPage(d), TabNavigation.Portfolio);
                   else
                       ChangeBaker(SelectedDelegation);

                   SelectedDelegation = null;
               });

            _ = LoadDelegationInfoAsync();

            _delegateViewModel = new DelegateViewModel(_app, _navigationService);
        }

        private async Task LoadDelegationInfoAsync()
        {
            try
            {
                var balance = await _app.Account
                    .GetBalanceAsync(Tezos.Name)
                    .ConfigureAwait(false);

                var addresses = await _app.Account
                    .GetUnspentAddressesAsync(Tezos.Name)
                    .ConfigureAwait(false);

                var rpc = new Rpc(Tezos.RpcNodeUri);

                var delegations = new List<DelegationViewModel>();

                var tzktApi = new TzktApi(Tezos);
                var head = await tzktApi.GetHeadLevelAsync();
                var headLevel = head.Value;

                var currentCycle = _app.Account.Network == Network.MainNet
                    ? Math.Floor((headLevel - 1) / 4096)
                    : Math.Floor((headLevel - 1) / 2048);

                foreach (var wa in addresses)
                {
                    var accountData = await rpc
                        .GetAccount(wa.Address)
                        .ConfigureAwait(false);

                    var @delegate = accountData["delegate"]?.ToString();

                    if (string.IsNullOrEmpty(@delegate))
                    {
                        delegations.Add(new DelegationViewModel(_navigationService)
                        {
                            Baker = null,
                            Address = wa.Address,
                            ExplorerUri = Tezos.BbUri,
                            Balance = wa.Balance,
                            DelegationTime = DateTime.Today,
                            Status = DelegationStatus.NotDelegated,
                            CopyAddress = CopyAddress,
                            ChangeBaker = ChangeBaker,
                            Undelegate = Undelegate
                        });

                        continue;
                    }

                    var baker = await BbApi
                        .GetBaker(@delegate, _app.Account.Network)
                        .ConfigureAwait(false) ?? new BakerData { Address = @delegate };

                    var account = await tzktApi.GetAccountByAddressAsync(wa.Address);

                    var txCycle = _app.Account.Network == Network.MainNet
                        ? Math.Floor((account.Value.DelegationLevel - 1) / 4096)
                        : Math.Floor((account.Value.DelegationLevel - 1) / 2048);

                    delegations.Add(new DelegationViewModel(_navigationService)
                    {
                        Baker = baker,
                        Address = wa.Address,
                        ExplorerUri = Tezos.BbUri,
                        Balance = wa.Balance,
                        DelegationTime = account.Value.DelegationTime,
                        Status = currentCycle - txCycle < 2 ? DelegationStatus.Pending :
                                currentCycle - txCycle < 7 ? DelegationStatus.Confirmed :
                                DelegationStatus.Active,
                        CopyAddress = CopyAddress,
                        ChangeBaker = ChangeBaker,
                        Undelegate = Undelegate
                    });
                }

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Delegations = new ObservableCollection<DelegationViewModel>(delegations);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadDelegationInfoAsync canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadDelegationInfoAsync error.");
            }
        }

        private void ChangeBaker(DelegationViewModel delegation)
        {
            _delegateViewModel?.InitializeWith(delegation);
            _navigationService?.ShowPage(new DelegatePage(_delegateViewModel), TabNavigation.Portfolio);
        }

        private void Undelegate(DelegationViewModel delegation)
        {
            _navigationService.SetInitiatedPage(TabNavigation.Portfolio);
            _delegateViewModel?.Undelegate(delegation.Address);
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name != args.Currency) return;

                await UpdateBalanceAsync();
                await LoadTransactionsAsync();
                await LoadDelegationInfoAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }
    }
}
