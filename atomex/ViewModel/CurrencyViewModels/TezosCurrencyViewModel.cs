using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
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
        private const int DelegationCheckIntervalInSec = 20;

        public bool IsStakingAvailable => CurrencyCode == "XTZ";
        public TezosConfig Tezos => Currency as TezosConfig;
        [Reactive] public ObservableCollection<Delegation> Delegations { get; set; }
        [Reactive] public Delegation SelectedDelegation { get; set; }
        private DelegateViewModel _delegateViewModel { get; set; }

        public TezosCurrencyViewModel(
             IAtomexApp app,
             CurrencyConfig currency,
             INavigationService navigationService)
            : base(app, currency, navigationService)
        {
            Delegations = new ObservableCollection<Delegation>();

            this.WhenAnyValue(vm => vm.SelectedDelegation)
               .WhereNotNull()
               .SubscribeInMainThread(d =>
               {
                   if (d.Baker != null)
                   {
                       _navigationService?.ShowPage(new DelegationInfoPage(d), TabNavigation.Portfolio);
                   }
                   else
                   {
                       _delegateViewModel.InitializeWith(SelectedDelegation);
                       _navigationService?.ShowPage(new DelegatePage(_delegateViewModel), TabNavigation.Portfolio);
                   }

                   SelectedDelegation = null;
               });

            _ = LoadDelegationInfoAsync();

            _delegateViewModel = new DelegateViewModel(_app, _navigationService);
            //_delegateViewModel = new DelegateViewModel(_app, async () =>
            //{
            //    await Task.Delay(TimeSpan.FromSeconds(DelegationCheckIntervalInSec))
            //        .ConfigureAwait(false);
            //    await Dispatcher.UIThread.InvokeAsync(OnUpdateClick);
            //});
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

                var delegations = new List<Delegation>();

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
                        delegations.Add(new Delegation
                        {
                            Baker = null,
                            Address = wa.Address.TruncateAddress(),
                            ExplorerUri = Tezos.BbUri,
                            Balance = wa.Balance,
                            DelegationTime = DateTime.Today,
                            Status = DelegationStatus.NotDelegated
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

                    delegations.Add(new Delegation
                    {
                        Baker = baker,
                        Address = wa.Address.TruncateAddress(),
                        ExplorerUri = Tezos.BbUri,
                        Balance = wa.Balance,
                        DelegationTime = account.Value.DelegationTime,
                        Status = currentCycle - txCycle < 2 ? DelegationStatus.Pending :
                                currentCycle - txCycle < 7 ? DelegationStatus.Confirmed :
                                DelegationStatus.Active
                    });
                }

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Delegations = new ObservableCollection<Delegation>(delegations);
                });

                //await Dispatcher.UIThread.InvokeAsync(() =>
                //{
                //    CanDelegate = balance.Available > 0;
                //    HasDelegations = delegations.Count > 0;
                //    SortDelegations(delegations);
                //},
                //    DispatcherPriority.Background);
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
