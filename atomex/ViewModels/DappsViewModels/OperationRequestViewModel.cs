using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex;
using atomex.Common;
using Atomex.MarketData.Abstract;
using atomex.Resources;
using Netezos.Forging.Models;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace atomex.ViewModels.DappsViewModels
{
 public abstract class BaseBeaconOperationViewModel : BaseViewModel, IDisposable
    {
        public int Id { get; set; }
        protected static string BaseCurrencyCode => "USD";
        public string BaseCurrencyFormat => "$0.##";
        public abstract string JsonStringOperation { get; }
        [Reactive] public IQuotesProvider QuotesProvider { get; set; }
        [Reactive] public bool IsDetailsOpened { get; set; }

        protected BaseBeaconOperationViewModel()
        {
            this.WhenAnyValue(vm => vm.QuotesProvider)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(quotesProvider =>
                {
                    quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
                    OnQuotesUpdatedEventHandler(quotesProvider, EventArgs.Empty);
                });
        }

        protected abstract void OnQuotesUpdatedEventHandler(object? sender, EventArgs args);

        private ReactiveCommand<Unit, Unit> _onOpenDetailsCommand;

        public ReactiveCommand<Unit, Unit> OnOpenDetailsCommand =>
            _onOpenDetailsCommand ??= ReactiveCommand.Create(() => { IsDetailsOpened = !IsDetailsOpened; });

        public void Dispose()
        {
            if (QuotesProvider != null)
                QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }
    }

    public class TransactionContentViewModel : BaseBeaconOperationViewModel
    {
        public TransactionContent Operation { get; set; }
        public override string JsonStringOperation => JsonConvert.SerializeObject(Operation, Formatting.Indented);
        public decimal AmountInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Amount));
        public decimal FeeInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Fee));
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
            AmountInBase = AmountInTez.SafeMultiply(xtzQuote?.Bid ?? 0);
            FeeInBase = FeeInTez.SafeMultiply(xtzQuote?.Bid ?? 0);
            Log.Debug("Quotes updated for beacon TransactionContent operation {Id}", Id);
        }
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
        public string Title => string.Format(AppResources.RequestFromDapp, DappName);
        public string SubTitle => string.Format(AppResources.ConfirmDappOperations, DappName);
        public string DappLogo { get; set; }

        [Reactive] public IEnumerable<BaseBeaconOperationViewModel> Operations { get; set; }
        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public OperationRequestViewModel()
        {
            OnConfirmCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);
        }

        public Func<Task> OnConfirm { get; set; }
        public Func<Task> OnReject { get; set; }

        private ReactiveCommand<Unit, Unit> _onConfirmCommand;

        public ReactiveCommand<Unit, Unit> OnConfirmCommand =>
            _onConfirmCommand ??= ReactiveCommand.CreateFromTask(async () => await OnConfirm());

        private ReactiveCommand<Unit, Unit> _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());

        private ReactiveCommand<string, Unit> _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>(data =>
        {
            try
            {
                // App.Clipboard.SetTextAsync(data);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        public void Dispose()
        {
            foreach (var operation in Operations)
            {
                operation.Dispose();
            }
        }
    }
}