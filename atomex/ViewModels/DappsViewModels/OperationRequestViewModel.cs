using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos.Internal;
using atomex.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using atomex.Models;
using atomex.Resources;
using Atomex.Wallet.Tezos;
using Atomex.Wallets.Tezos;
using Netezos.Forging.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Xamarin.Essentials;
using DecimalExtensions = Atomex.Common.DecimalExtensions;

namespace atomex.ViewModels.DappsViewModels
{
    public abstract class BaseBeaconOperationViewModel : BaseViewModel, IDisposable
    {
        public int Id { get; set; }
        protected static string BaseCurrencyCode => "USD";
        public abstract string JsonStringOperation { get; }
        [Reactive] public IQuotesProvider QuotesProvider { get; set; }

        // [Reactive] public string CopyButtonName { get; set; }
        // [Reactive] public string DetailsButtonName { get; set; }
        // [ObservableAsProperty] public bool IsCopied { get; }

        protected BaseBeaconOperationViewModel()
        {
            // CopyCommand
            //     .IsExecuting
            //     .ToPropertyExInMainThread(this, vm => vm.IsCopied);

            this.WhenAnyValue(vm => vm.QuotesProvider)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(quotesProvider =>
                {
                    quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
                    OnQuotesUpdatedEventHandler(quotesProvider, EventArgs.Empty);
                });

            // CopyButtonName = AppResources.CopyButton;
            // DetailsButtonName = AppResources.DisplayTxDetails;
        }

        protected abstract void OnQuotesUpdatedEventHandler(object sender, EventArgs args);

        // private ReactiveCommand<string, Unit> _copyCommand;
        //
        // public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.CreateFromTask<string>(
        //     async data =>
        //     {
        //         try
        //         {
        //             CopyButtonName = AppResources.Copied;
        //             await Clipboard.SetTextAsync(data);
        //             await Task.Delay(1500);
        //             CopyButtonName = AppResources.CopyButton;
        //         }
        //         catch (Exception e)
        //         {
        //             CopyButtonName = AppResources.CopyButton;
        //             Log.Error(e, "Copy to clipboard error");
        //         }
        //     });

        public void Dispose()
        {
            if (QuotesProvider != null)
                QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }
    }

    public class TransactionContentViewModel : BaseBeaconOperationViewModel
    {
        [Reactive] public TransactionContent Operation { get; set; }
        public override string JsonStringOperation => JsonConvert.SerializeObject(Operation, Formatting.Indented);

        public string TezosFormat =>
            DecimalExtensions.GetFormatWithPrecision((int) Math.Round(Math.Log10(TezosConfig.XtzDigitsMultiplier)));

        public decimal AmountInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Amount));
        public decimal FeeInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Fee));
        public string DestinationIcon => $"https://services.tzkt.io/v1/avatars/{Operation.Destination}";
        [Reactive] public string DestinationAlias { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [ObservableAsProperty] public string Entrypoint { get; }
        public string ExplorerUri { get; set; }

        public TransactionContentViewModel()
        {
            this.WhenAnyValue(vm => vm.Operation)
                .WhereNotNull()
                .SubscribeInMainThread(async operation =>
                {
                    DestinationAlias = operation.Destination.TruncateAddress();
                    var url = $"v1/accounts/{operation.Destination}?metadata=true";

                    try
                    {
                        using var response = await HttpHelper.GetAsync(
                            baseUri: "https://api.tzkt.io/",
                            relativeUri: url);

                        if (response.StatusCode != HttpStatusCode.OK) return;

                        var responseContent = await response
                            .Content
                            .ReadAsStringAsync();

                        var responseJObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        var alias = responseJObj?["metadata"]?["alias"]?.ToString();
                        if (alias != null)
                            DestinationAlias = alias;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during sending request to {Url}", url);
                    }
                });

            this.WhenAnyValue(vm => vm.Operation)
                .WhereNotNull()
                .Select(operation =>
                    operation.Parameters == null ? "Transfer to " : $"Call {operation.Parameters.Entrypoint} in ")
                .ToPropertyExInMainThread(this, vm => vm.Entrypoint);
        }

        protected override void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
            AmountInBase = AmountInTez.SafeMultiply(xtzQuote?.Bid ?? 0);
            FeeInBase = FeeInTez.SafeMultiply(xtzQuote?.Bid ?? 0);
            Log.Debug("Quotes updated for beacon TransactionContent operation {Id}", Id);
        }

        private ReactiveCommand<Unit, Unit> _openDestinationInExplorer;
        
        public ReactiveCommand<Unit, Unit> OpenDestinationInExplorer => _openDestinationInExplorer ??=
            ReactiveCommand.Create(() =>
            {
                if (Uri.TryCreate($"{ExplorerUri}{Operation.Destination}", UriKind.Absolute,
                        out var uri))
                    Launcher.OpenAsync(new Uri(uri.ToString()));
            });
    }

    public class RevealContentViewModel : BaseBeaconOperationViewModel
    {
        public RevealContent Operation { get; set; }
        public override string JsonStringOperation => JsonConvert.SerializeObject(Operation, Formatting.Indented);
        public decimal FeeInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Fee));
        [Reactive] public decimal FeeInBase { get; set; }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
            FeeInBase = FeeInTez.SafeMultiply(xtzQuote?.Bid ?? 0);
            Log.Debug("Quotes updated for beacon RevealContent operation {Id}", Id);
        }
    }

    public class OperationRequestViewModel : BaseViewModel, IDisposable
    {
        public string DappName { get; set; }
        public string DappLogo { get; set; }

        public WalletAddress ConnectedWalletAddress { get; set; }
        public string Title => string.Format(AppResources.DappRequestToConfirm, DappName);
        [Reactive] public OperationRequestTab SelectedTab { get; set; }
        [Reactive] public bool IsDetailsOpened { get; set; }

        public string TezosFormat =>
            DecimalExtensions.GetFormatWithPrecision((int) Math.Round(Math.Log10(TezosConfig.XtzDigitsMultiplier)));

        [Reactive] public IEnumerable<BaseBeaconOperationViewModel> Operations { get; set; }
        private IEnumerable<ManagerOperationContent> _initialOperations { get; set; }
        [Reactive] private byte[] _forgedOperations { get; set; }
        [ObservableAsProperty] public string OperationsBytes { get; set; }
        [Reactive] public BaseBeaconOperationViewModel SelectedOperation { get; set; }
        [Reactive] public decimal TotalFees { get; set; }
        [Reactive] public decimal TotalFeesInBase { get; set; }
        [Reactive] public int TotalGasLimit { get; set; }
        [Reactive] public int TotalStorageLimit { get; set; }
        [Reactive] public decimal TotalGasFee { get; set; }
        [Reactive] public decimal TotalGasFeeInBase { get; set; }
        [Reactive] public decimal TotalStorageFee { get; set; }
        [Reactive] public decimal TotalStorageFeeInBase { get; set; }
        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public bool AutofillError { get; set; }
        [Reactive] public IQuotesProvider QuotesProvider { get; set; }
        private TezosConfig _tezos { get; }
        private static string _baseCurrencyCode => "USD";
        private static string _baseCurrencyFormat => "$0.##";
        private int? _byteCost { get; set; }
        private int _defaultOperationGasLimit { get; }
        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public OperationRequestViewModel(
            IEnumerable<ManagerOperationContent> operations,
            WalletAddress connectedAddress,
            int operationGasLimit,
            TezosConfig tezosConfig)
        {
            _tezos = tezosConfig;
            ConnectedWalletAddress = connectedAddress;
            _initialOperations = operations;
            _defaultOperationGasLimit = operationGasLimit;

            OnConfirmCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);

            this.WhenAnyValue(vm => vm.QuotesProvider)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(quotesProvider =>
                {
                    quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
                });

            this.WhenAnyValue(vm => vm.Operations, vm => vm.QuotesProvider)
                .WhereAllNotNull()
                .Select(args => args.Item1.ToList())
                .SubscribeInMainThread(async operations =>
                {
                    TotalGasLimit = operations.Aggregate(0, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.Operation.GasLimit,
                        RevealContentViewModel revealOp => res + revealOp.Operation.GasLimit,
                        _ => res
                    });

                    TotalStorageLimit = operations.Aggregate(0, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.Operation.StorageLimit,
                        RevealContentViewModel revealOp => res + revealOp.Operation.StorageLimit,
                        _ => res
                    });

                    TotalGasFee = operations.Aggregate(0m, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.FeeInTez,
                        RevealContentViewModel revealOp => res + revealOp.FeeInTez,
                        _ => res
                    });

                    const string url = "v1/protocols/current";
                    try
                    {
                        using var response = await HttpHelper.GetAsync(
                            baseUri: "https://api.tzkt.io/",
                            relativeUri: url);

                        if (response.StatusCode != HttpStatusCode.OK) return;

                        var responseContent = await response
                            .Content
                            .ReadAsStringAsync();

                        var responseJObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        if (!int.TryParse(responseJObj?["constants"]?["byteCost"]?.ToString(), out var byteCost))
                            return;

                        _byteCost = byteCost;
                        TotalStorageFee = TezosConfig.MtzToTz(Convert.ToDecimal(byteCost)) * TotalStorageLimit;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during sending request to {Url}", url);
                    }
                });

            this.WhenAnyValue(vm => vm.TotalGasFee, vm => vm.TotalStorageFee)
                .WhereAllNotNull()
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Select(values => values.Item1 + values.Item2)
                .SubscribeInMainThread(totalFees =>
                {
                    TotalFees = totalFees;
                    OnQuotesUpdatedEventHandler(QuotesProvider, EventArgs.Empty);
                });

            this.WhenAnyValue(vm => vm.TotalStorageLimit)
                .WhereNotNull()
                .Skip(1)
                .SubscribeInMainThread(totalStorageLimit =>
                {
                    if (_byteCost != null)
                        TotalStorageFee = TezosConfig.MtzToTz(Convert.ToDecimal(_byteCost)) * totalStorageLimit;
                });

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .WhereNotNull()
                .Where(useDefaultFee => useDefaultFee)
                .SubscribeInMainThread(_ => { AutofillOperations(); });

            this.WhenAnyValue(vm => vm.TotalGasFee, vm => vm.TotalStorageLimit)
                .WhereAllNotNull()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Where(_ => !UseDefaultFee)
                .SubscribeInMainThread(_ => { AutofillOperations(); });

            this.WhenAnyValue(vm => vm._forgedOperations)
                .WhereNotNull()
                .Select(forgedOperations => forgedOperations.ToHexString())
                .ToPropertyExInMainThread(this, vm => vm.OperationsBytes);

            SelectedTab = OperationRequestTab.Preview;
            UseDefaultFee = true;
        }

        private async void AutofillOperations()
        {
            AutofillError = false;

            var rpc = new Rpc(_tezos.RpcNodeUri);
            var head = await rpc
                .GetHeader()
                .ConfigureAwait(false);

            var operations = _initialOperations.Select(op => op switch
                {
                    TransactionContent txContent => new TransactionContent
                    {
                        Amount = txContent.Amount,
                        Destination = txContent.Destination,
                        Parameters = txContent.Parameters,
                        Source = txContent.Source,
                        Fee = UseDefaultFee ? 0 : txContent.Fee,
                        Counter = txContent.Counter,
                        GasLimit = UseDefaultFee ? _defaultOperationGasLimit : txContent.GasLimit,
                        StorageLimit = UseDefaultFee ? DappsViewModel.StorageLimitPerOperation : txContent.StorageLimit
                    },
                    RevealContent revealContent => new RevealContent
                    {
                        PublicKey = revealContent.PublicKey,
                        Source = revealContent.Source,
                        Fee = UseDefaultFee ? 0 : revealContent.Fee,
                        Counter = revealContent.Counter,
                        GasLimit = UseDefaultFee ? _defaultOperationGasLimit : revealContent.GasLimit,
                        StorageLimit = UseDefaultFee
                            ? DappsViewModel.StorageLimitPerOperation
                            : revealContent.StorageLimit
                    },
                    _ => op
                })
                .ToList();

            if (!UseDefaultFee)
            {
                var avgFee = Convert.ToInt64(TotalGasFee * TezosConfig.XtzDigitsMultiplier / operations.Count);

                foreach (var op in operations)
                {
                    op.Fee = avgFee;
                }

                if (TotalStorageLimit != 0)
                {
                    foreach (var op in operations.Where(op => op.StorageLimit != 0))
                    {
                        op.StorageLimit = TotalStorageLimit;
                        break;
                    }
                }
            }

            var error = await TezosOperationFiller.AutoFillAsync(
                    operations,
                    head["hash"]!.ToString(),
                    head["chain_id"]!.ToString(),
                    _tezos)
                .ConfigureAwait(false);

            if (error != null)
                AutofillError = true;

            _forgedOperations = await TezosForge.ForgeAsync(
                operations: operations,
                branch: head["hash"]!.ToString());

            if (!UseDefaultFee && Operations != null) return;

            var operationsViewModel = new ObservableCollection<BaseBeaconOperationViewModel>();
            foreach (var item in operations.Select((value, idx) => new {idx, value}))
            {
                var operation = item.value;
                var index = item.idx;

                switch (operation)
                {
                    case TransactionContent transactionOperation:
                        operationsViewModel.Add(new TransactionContentViewModel
                        {
                            Id = index + 1,
                            Operation = transactionOperation,
                            QuotesProvider = QuotesProvider,
                            ExplorerUri = _tezos.AddressExplorerUri
                        });
                        break;
                    case RevealContent revealOperation:
                        operationsViewModel.Add(new RevealContentViewModel
                        {
                            Id = index + 1,
                            Operation = revealOperation,
                            QuotesProvider = QuotesProvider,
                        });
                        break;
                }
            }

            if (Operations != null)
            {
                foreach (var op in Operations)
                {
                    if (op is IDisposable disposable)
                        disposable.Dispose();
                }
            }

            _initialOperations = operations;
            Operations = operationsViewModel;
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, _baseCurrencyCode);
            TotalGasFeeInBase = TotalGasFee.SafeMultiply(xtzQuote?.Bid ?? 0);
            TotalStorageFeeInBase = TotalStorageFee.SafeMultiply(xtzQuote?.Bid ?? 0);
            TotalFeesInBase = TotalFees.SafeMultiply(xtzQuote?.Bid ?? 0);

            Log.Debug("Quotes updated for Operation Request View");
        }

        public Func<Task> OnConfirm { get; set; }
        public Func<Task> OnReject { get; set; }

        private ReactiveCommand<Unit, Unit> _onConfirmCommand;

        public ReactiveCommand<Unit, Unit> OnConfirmCommand =>
            _onConfirmCommand ??= ReactiveCommand.CreateFromTask(async () => await OnConfirm());

        private ReactiveCommand<Unit, Unit> _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());

        private ReactiveCommand<string, Unit> _changeTabCommand;

        public ReactiveCommand<string, Unit> ChangeTabCommand => _changeTabCommand ??=
            ReactiveCommand.Create<string>(value =>
            {
                Enum.TryParse(value, out OperationRequestTab selectedTab);
                SelectedTab = selectedTab;
            });

        private ReactiveCommand<Unit, Unit> _onOpenDetailsCommand;

        public ReactiveCommand<Unit, Unit> OnOpenDetailsCommand =>
            _onOpenDetailsCommand ??= ReactiveCommand.Create(() => { IsDetailsOpened = !IsDetailsOpened; });
        
        public void Dispose()
        {
            foreach (var operation in Operations)
            {
                operation.Dispose();
            }
        }
    }
}