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
            Log.Debug("UpdateTransactionsAsync for {@currency}", Currency.Name);

            try
            {
                if (_app.Account == null)
                    return;

                var fa12currency = Currency as Fa12Config;

                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                var transactions = (await _app.Account
                        .GetCurrencyAccount<Fa12Account>(Currency.Name)
                        .DataRepository
                        .GetTezosTokenTransfersAsync(fa12currency.TokenContractAddress)
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
                            .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key,
                                new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))))
                        : Transactions
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key,
                                new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

                    GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionsAsync error for {@currency}.", Currency?.Name);
            }
        }

        protected override void OnReceiveClick()
        {
            var tezosConfig = _app.Account.Currencies.GetByName(TezosConfig.Xtz);
            string tokenContractAddress = (Currency as Fa12Config).TokenContractAddress;

            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                currency: tezosConfig,
                navigationService: _navigationService,
                tokenContract: tokenContractAddress,
                tokenType: "FA12");
            _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(receiveViewModel));
        }

        public override async Task ScanCurrency()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            IsRefreshing = true;

            try
            {
                await _app.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .UpdateBalanceAsync(_cancellationTokenSource.Token);

                await LoadTransactionsAsync();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(MessageType.Regular,
                        Currency.Description + " " + AppResources.HasBeenUpdated);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "Fa12WalletViewModel.OnUpdateClick error.");
                // todo: message to user!?
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        protected override void LoadAddresses()
        {
            var fa12currency = Currency as Fa12Config;

            AddressesViewModel = new AddressesViewModel(
                app: _app,
                currency: fa12currency,
                navigationService: _navigationService,
                tokenContract: fa12currency.TokenContractAddress);
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