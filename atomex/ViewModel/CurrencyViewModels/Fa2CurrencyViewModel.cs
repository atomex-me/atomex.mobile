using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using atomex.Resources;
using atomex.ViewModel.TransactionViewModels;
using atomex.Views;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.TezosTokens;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Forms;
using static atomex.Models.SnackbarMessage;

namespace atomex.ViewModel.CurrencyViewModels
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
            Log.Debug("LoadTransactionsAsync for FA2 {@currency}.", Currency.Name);

            try
            {
                if (_app.Account == null)
                    return;

                var fa2currency = Currency as Fa2Config;

                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                var transactions = (await _app.Account
                    .GetCurrencyAccount<Fa2Account>(Currency.Name)
                    .DataRepository
                    .GetTezosTokenTransfersAsync(fa2currency.TokenContractAddress, offset: 0, limit: int.MaxValue)
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
                            .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))))
                        : Transactions
                            .GroupBy(p => p.LocalTime.Date)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, new ObservableCollection<TransactionViewModel>(g.OrderByDescending(g => g.LocalTime))));

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
            string tokenContractAddress = (Currency as Fa2Config).TokenContractAddress;

            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                currency: tezosConfig,
                navigationService: _navigationService,
                tokenContract: tokenContractAddress,
                tokenType: "FA2");
            _navigationService?.ShowBottomSheet(new ReceiveBottomSheet(receiveViewModel));
        }

        public override async Task ScanCurrency()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            IsRefreshing = true;

            try
            {
                await _app.Account
                    .GetCurrencyAccount<Fa2Account>(Currency.Name)
                    .UpdateBalanceAsync(_cancellationTokenSource.Token);

                await LoadTransactionsAsync();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    _navigationService?.DisplaySnackBar(MessageType.Regular, Currency.Description + " " + AppResources.HasBeenUpdated);
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
            var fa2currency = Currency as Fa2Config;

            AddressesViewModel = new AddressesViewModel(
                app: _app,
                currency: fa2currency,
                navigationService: _navigationService,
                tokenContract: fa2currency.TokenContractAddress);
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                var tezosTokenConfig = (TezosTokenConfig)Currency;

                if (!args.IsTokenUpdate ||
                   args.TokenContract != null && (args.TokenContract != tezosTokenConfig.TokenContractAddress || args.TokenId != tezosTokenConfig.TokenId))
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
