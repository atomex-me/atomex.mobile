using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using atomex.Views;
using atomex.Models;
using atomex.Resources;
using Atomex.Wallet.Abstract;
using Netezos.Forging.Models;
using Serilog;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class OperationRequestViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;

        public INavigation Navigation { get; set; }

        private ObservableCollection<Transaction> _operations;

        public ObservableCollection<Transaction> Operations
        {
            get => _operations;
            set { _operations = value; OnPropertyChanged(nameof(_operations)); }
        }

        public decimal SumFeeOperations => _operations.Sum(x => x.SumFee);

        public bool IsVisibleExtraTransactionDetails { get; set; }
        public string DisplayExtraTransactionDetails => JsonSerializer.Serialize(_operations);

        public OperationRequestViewModel(IAtomexApp app, INavigation navigation)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));;
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            LoadOperations();
        }

        void LoadOperations()
        {
            Operations = new ObservableCollection<Transaction>()
            {
                new() { Amount = 1, Counter = 1, Destination = "tzx32434fdsfvcxxxvf12", Fee = 1, GasLimit = 100, Source = "tzxfsd324ekrmldmfdsf"},
                new() { Amount = 2, Counter = 2, Destination = "tzx32434fdsfvcxxxv32f", Fee = 2, GasLimit = 100, Source = "tzxfsd324ekrmldmfdsf"},
                new() { Amount = 3, Counter = 3, Destination = "tzx32434fdsfvcxxxv432f", Fee = 3, GasLimit = 100, Source = "tzxfsd324ekrmldmfdsf"},

            };
        }

        private ICommand _selectEditOperationClickedCommand;

        public ICommand SelectOperationTappedCommand => _selectEditOperationClickedCommand ??= new Command<Transaction>(async (transaction) => await OnEditOperationTapped(transaction));

        
        private ICommand _acceptOperationsCommand;
        public ICommand AcceptOperationsCommand => _acceptOperationsCommand ??= new Command(async () => await ConfirmSendRequestOperations());
        
        private ICommand _cancelOperationsCommand;
        public ICommand CancelOperationsCommand => _cancelOperationsCommand ??= new Command(async () => await ClosePopup());

        private ICommand _displayExtraTransactionDetailsCommand;
        public ICommand DisplayExtraTransactionDetailsCommand => _displayExtraTransactionDetailsCommand ??= new Command(OnDisplayTransactionDetailsClicked);

        private void OnDisplayTransactionDetailsClicked()
        {
            IsVisibleExtraTransactionDetails = !IsVisibleExtraTransactionDetails;
            OnPropertyChanged(nameof(IsVisibleExtraTransactionDetails));

        }

        private async Task ConfirmSendRequestOperations()
        {
            var account = _app.Account.GetCurrencyAccount<ILegacyCurrencyAccount>("BTC/TZC");
            try
            {
                // Operations.Select(async x => await account.SendAsync(x.Destination, x.Amount, x.Fee, x.))

                var confirm = await Application.Current.MainPage.DisplayAlert("Success", "Your transaction has been successfully broadcasted", "OK", "Open Tzkt Explorer");
                if (confirm)
                {
                    await ClosePopup();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction send error.");
                await Application.Current.MainPage.DisplayAlert(AppResources.Error, AppResources.SendingTransactionError, AppResources.AcceptButton);
            }
            
        }
        private async Task ClosePopup()
        {
            await Navigation.PopAsync();
        }

        private async Task OnEditOperationTapped(Transaction transaction)
        {
            await Navigation.PushAsync(new OperationRequestEditPage(new EditOperationViewModel(
                _app,
                Navigation,
                transaction
                )));
            OnPropertyChanged(nameof(_operations));
        }
    }

    public class BeaconMessageEventArgs : EventArgs
    {
        
    }

    public interface IWalletBeaconClient
    {
        event EventHandler<BeaconMessageEventArgs> OnBeaconMessageReceived;
        
        string AppName { get; }

        Task InitAsync();

        // Task AddPeerAsync(P2PPairingRequest pairingRequest, bool sendPairingResponse = true);

        void Connect();

        void Disconnect();
    }
}