using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.Views;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.TezosTokens;
using atomex.ViewModels.TransactionViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class Fa2CurrencyViewModel : CurrencyViewModel
    {
        public Fa2CurrencyViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            INavigationService navigationService)
            : base(app, currency, navigationService)
        {
        }

        public override async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for FA2 {@Currency}", Currency.Name);

            try
            {
                if (App.Account == null)
                    return;

                var fa2Currency = Currency as Fa2Config;

                var tezosConfig = App.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                var txs = await Task.Run(async () =>
                {
                    var transactions = (await App.Account
                            .GetCurrencyAccount<Fa2Account>(Currency.Name)
                            .DataRepository
                            .GetTezosTokenTransfersAsync(fa2Currency?.TokenContractAddress, offset: 0, limit: int.MaxValue)
                            .ConfigureAwait(false))
                        .ToList();
                    
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
                    
                    return Transactions
                        .Take(QtyDisplayedTxs <= DefaultQtyDisplayedTxs
                            ? DefaultQtyDisplayedTxs
                            : QtyDisplayedTxs)
                        .ToList();;
                });
                
                var groups = txs
                    .GroupBy(p => p.LocalTime.Date)
                    .Select(g => new Grouping<TransactionViewModel>(g.Key,
                        new ObservableCollection<TransactionViewModel>(g)))
                    .ToList();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    GroupedTransactions = new ObservableCollection<Grouping<TransactionViewModel>>(groups);
                    QtyDisplayedTxs = txs.Count;
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
            var tokenContractAddress = (Currency as Fa2Config)?.TokenContractAddress;

            var receiveViewModel = new ReceiveViewModel(
                app: App,
                currency: tezosConfig,
                navigationService: NavigationService,
                tokenContract: tokenContractAddress,
                tokenType: "FA2");
            
            NavigationService?.ShowPopup(new ReceiveBottomSheet(receiveViewModel));
        }

        public override async Task ScanCurrency()
        {
            CancellationTokenSource = new CancellationTokenSource();
            IsRefreshing = true;

            try
            {
                await App.Account
                    .GetCurrencyAccount<Fa2Account>(Currency.Name)
                    .UpdateBalanceAsync(CancellationTokenSource.Token);

                await Task.Run(async () => await LoadTransactionsAsync());

                await Device.InvokeOnMainThreadAsync(() =>
                    NavigationService?.DisplaySnackBar(
                        MessageType.Regular,
                        Currency.Description + " " + AppResources.HasBeenUpdated));
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

        public override void LoadAddresses()
        {
            var fa2Currency = Currency as Fa2Config;

            AddressesViewModel = new AddressesViewModel(
                app: App,
                currency: fa2Currency,
                navigationService: NavigationService,
                tokenContract: fa2Currency?.TokenContractAddress);
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                var tezosTokenConfig = (TezosTokenConfig) Currency;

                if (!args.IsTokenUpdate ||
                    args.TokenContract != null && (args.TokenContract != tezosTokenConfig.TokenContractAddress ||
                                                   args.TokenId != tezosTokenConfig.TokenId)) return;

                await Task.Run(async () =>
                {
                    await UpdateBalanceAsync();
                    await LoadTransactionsAsync();
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }
    }
}