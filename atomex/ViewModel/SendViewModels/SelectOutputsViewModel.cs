using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Atomex;
using Atomex.Blockchain.BitcoinBased;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace atomex.ViewModel.SendViewModels
{
    public class SelectOutputsViewModel : BaseViewModel
    {
        [Reactive] public ObservableCollection<OutputViewModel> Outputs { get; set; }
        [Reactive] public int SelectedOutputsNumber { get; set; }
        [Reactive] public decimal SelectedAmount { get; set; }
        public Action<IEnumerable<BitcoinBasedTxOutput>> ConfirmAction { get; set; }

        public SelectOutputsViewModel()
        {
            this.WhenAnyValue(vm => vm.SelectAll)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Where(_ => Outputs != null && !_selectFromList)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    SelectAllOutputs();
                    UpdateSelectedAmount();
                });

            this.WhenAnyValue(vm => vm.SortIsAscending)
                .Where(_ => Outputs != null)
                .Subscribe(sortIsAscending =>
                {
                    Outputs = sortIsAscending
                        ? new ObservableCollection<OutputViewModel>(
                            Outputs.OrderBy(output => output.Balance))
                        : new ObservableCollection<OutputViewModel>(
                            Outputs.OrderByDescending(output => output.Balance));
                });

            //this.WhenAnyValue(vm => vm.Outputs)
            //    .WhereNotNull()
            //    .Take(1)
            //    .Subscribe(outputs =>
            //        Outputs = new ObservableCollection<OutputViewModel>(
            //            outputs.OrderByDescending(output => output.Balance)));

            //Outputs.ToObservableChangeSet()
            //    .AutoRefresh()
            //    .Subscribe(vm =>
            //         Test1());

            //this.Outputs
            //     .ToObservableChangeSet(x => x)
            //     .ToCollection()
            //     .Select(items => items.Any())
            //     .Subscribe(x => Test());

            //this.WhenAnyValue(vm => vm.Outputs)
            //    .WhereNotNull()
            //    .Subscribe(outputs =>
            //    {
            //        outputs
            //            .ToObservableChangeSet()
            //            .Subscribe(o =>
            //            {
            //                Test1();
            //            });
            //    });
        }

        [Reactive] public bool SelectAll { get; set; }
        [Reactive] public bool SortByBalance { get; set; }
        [Reactive] public bool SortIsAscending { get; set; }

        private bool _selectFromList;

        private ReactiveCommand<Unit, Unit> _changeSortTypeCommand;
        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));

        private ReactiveCommand<OutputViewModel, Unit> _selectOutputCommand;
        public ReactiveCommand<OutputViewModel, Unit> SelectOutputCommand => _selectOutputCommand ??=
            (_selectOutputCommand = ReactiveCommand.Create<OutputViewModel>(o => SelectOutput(o)));

        private ReactiveCommand<Unit, Unit> _selectAllCommand;
        public ReactiveCommand<Unit, Unit> SelectAllCommand => _selectAllCommand ??=
            (_selectAllCommand = ReactiveCommand.Create(SelectAllOutputs));

        private ICommand _confirmOutputsCommand;
        public ICommand ConfirmOutputsCommand => _confirmOutputsCommand ??=
            (_confirmOutputsCommand = ReactiveCommand.Create(() =>
            {
                var outputs = Outputs
                    .Where(output => output.IsSelected)
                    .Select(o => o.Output);

                ConfirmAction?.Invoke(outputs);
            }));

        private void SelectAllOutputs()
        {
            _selectFromList = false;
            Outputs.ToList().ForEach(o => o.IsSelected = SelectAll);
        }

        private void UpdateSelectedAmount()
        {
            SelectedOutputsNumber = Outputs
                .Where(o => o.IsSelected).Count();

            SelectedAmount = Outputs
                .Where(o => o.IsSelected)
                .Aggregate((decimal)0, (sum, output) => sum + output.Balance);
        }

        private void SelectOutput(OutputViewModel vm)
        {
            vm.IsSelected = !vm.IsSelected;

            _selectFromList = true;
            var selectionResult = Outputs.Aggregate(true, (result, output) => result && output.IsSelected);
            if (SelectAll != selectionResult)
                SelectAll = selectionResult;
            
            UpdateSelectedAmount();
        }
    }


    public class OutputViewModel : BaseViewModel
    {
        [Reactive] public bool IsSelected { get; set; }
        public BitcoinBasedTxOutput Output { get; set; }
        public BitcoinBasedConfig Config { get; set; }

        public Action<string> CopyAction { get; set; }

        public decimal Balance =>
            Config.SatoshiToCoin(Output.Value);

        public string Address => Output.DestinationAddress(Config.Network);

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??=
            (_copyCommand = ReactiveCommand.Create(() => { CopyAction?.Invoke(Address); }));
    }
}

