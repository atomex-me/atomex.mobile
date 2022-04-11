using System;
using System.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;
using Serilog;
using Xamarin.Forms;
using atomex.ViewModel.TransactionViewModels;
using System.Collections.ObjectModel;
using Atomex.Common;
using Atomex.Core;
using System.Windows.Input;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class Fa12CurrencyViewModel : CurrencyViewModel
    {

        public Fa12CurrencyViewModel(
           IAtomexApp app, CurrencyConfig currency)
           : base(app, currency)
        {
        }


        public override async Task UpdateTransactionsAsync()
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
                            }));
                    var groups = Transactions.GroupBy(p => p.LocalTime.Date).Select(g => new Grouping<DateTime, TransactionViewModel>(g.Key, g));
                    GroupedTransactions = new ObservableCollection<Grouping<DateTime, TransactionViewModel>>(groups);

                    OnPropertyChanged(nameof(Transactions));
                    OnPropertyChanged(nameof(GroupedTransactions));
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


        //public override ICommand ReceivePageCommand => _receivePageCommand ??= new Command(async () => await OnReceiveButtonClicked());

        //public override ICommand AddressesPageCommand => _addressesPageCommand ??= new Command(async () => await OnAddressesButtonClicked());

        private async Task OnReceiveButtonClicked()
        {
            var fa12currency = Currency as Fa12Config;
            var tezosConfig = _app.Account
                .Currencies
                .GetByName(TezosConfig.Xtz);

            await Navigation.PushAsync(new ReceivePage(new ReceiveViewModel(_app, tezosConfig, Navigation, fa12currency.TokenContractAddress)));
        }

        private async Task OnAddressesButtonClicked()
        {
            var fa12currency = Currency as Fa12Config;
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            await Navigation.PushAsync(new AddressesPage(new AddressesViewModel(_app, tezosConfig, fa12currency.TokenContractAddress)));
        }

    }
}
