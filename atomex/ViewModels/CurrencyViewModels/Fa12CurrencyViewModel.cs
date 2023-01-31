using System;
using System.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using Atomex.Common;
using Atomex.Core;
using atomex.Views;
using System.Threading;
using static atomex.Models.SnackbarMessage;
using atomex.Resources;
using atomex.ViewModels.TransactionViewModels;
using Atomex.Wallet;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class Fa12CurrencyViewModel : CurrencyViewModel
    {
        public Fa12CurrencyViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            INavigationService navigationService)
            : base(app, currency, navigationService)
        {
        }

        public override async Task LoadTransactionsAsync()
        {
            Log.Debug("UpdateTransactionsAsync for {@Currency}", Currency.Name);

            try
            {
                if (App.Account == null)
                    return;

                var fa12Currency = Currency as Fa12Config;

                var tezosConfig = App.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                var transactions = (await App.Account
                        .GetCurrencyAccount<Fa12Account>(Currency.Name)
                        .DataRepository
                        .GetTezosTokenTransfersAsync(fa12Currency?.TokenContractAddress)
                        .ConfigureAwait(false))
                    .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                        transactions.Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                            .ToList()
                            .SortList((t1, t2) => t2.LocalTime.CompareTo(t1.LocalTime))
                            .ForEachDo(t =>
                            {
                                t.RemoveClicked += RemoveTransactonEventHandler;
                                t.CopyAddress = CopyAddress;
                                t.CopyTxId = CopyTxId;
                            }));

                    var groups = !IsAllTxsShowed
                        ? Transactions
                            .OrderByDescending(p => p.LocalTime.Date)
                            .Take(TxsNumberPerPage)
                            .GroupBy(p => p.LocalTime.Date)
                            .Select(g => new Grouping<TransactionViewModel>(g.Key,
                                new ObservableCollection<TransactionViewModel>(g.OrderByDescending(t => t.LocalTime))))
                        : Transactions
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<TransactionViewModel>(g.Key,
                                new ObservableCollection<TransactionViewModel>(g.OrderByDescending(t => t.LocalTime))));

                    GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(groups);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionsAsync error for {@Currency}", Currency?.Name);
            }
        }

        protected override void OnReceiveClick()
        {
            var tezosConfig = App.Account.Currencies.GetByName(TezosConfig.Xtz);
            string tokenContractAddress = (Currency as Fa12Config)?.TokenContractAddress;

            var receiveViewModel = new ReceiveViewModel(
                app: App,
                currency: tezosConfig,
                navigationService: NavigationService,
                tokenContract: tokenContractAddress,
                tokenType: "FA12");
            NavigationService?.ShowPopup(new ReceiveBottomSheet(receiveViewModel));
        }

        public override async Task ScanCurrency()
        {
            CancellationTokenSource = new CancellationTokenSource();
            IsRefreshing = true;

            try
            {
                await App.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .UpdateBalanceAsync(CancellationTokenSource.Token);

                await LoadTransactionsAsync();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    NavigationService?.DisplaySnackBar(MessageType.Regular,
                        Currency.Description + " " + AppResources.HasBeenUpdated);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Fa12WalletViewModel.OnUpdateClick error");
                // todo: message to user!?
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        protected override void LoadAddresses()
        {
            var fa12Currency = Currency as Fa12Config;

            AddressesViewModel = new AddressesViewModel(
                app: App,
                currency: fa12Currency,
                navigationService: NavigationService,
                tokenContract: fa12Currency?.TokenContractAddress);
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                var tezosTokenConfig = (TezosTokenConfig) Currency;

                if (!args.IsTokenUpdate ||
                    args.TokenContract != null && (args.TokenContract != tezosTokenConfig.TokenContractAddress ||
                                                   args.TokenId != tezosTokenConfig.TokenId))
                {
                    return;
                }

                await UpdateBalanceAsync();
                await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }
    }
}