using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using atomex.Models;
using atomex.Views;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public class EditOperationViewModel: BaseViewModel
    {
        private readonly IAtomexApp _app;

        public INavigation Navigation { get; set; }

        private Transaction _operation;

        public Transaction Operation
        {
            get => _operation;
            set { _operation = value; OnPropertyChanged(nameof(_operation)); }
        }

        public EditOperationViewModel(IAtomexApp app, INavigation navigation, Transaction transaction)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));;
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Operation = transaction;
        }

        private ICommand _closePopupCommand;
        public ICommand ClosePopupCommand => _closePopupCommand ??= new Command(async () => await ClosePopup());

        private ICommand _acceptPopupCommand;
        public ICommand AcceptPopupCommand => _acceptPopupCommand ??= new Command(async () => await SaveEditedOperation());

        private async Task SaveEditedOperation()
        {
            await Navigation.PopAsync();
            IReadOnlyList<Page> navStack = Navigation.NavigationStack;

            if (navStack[navStack.Count - 1] is OperationRequestListPage operationRequestListPage)
            {
                var operationRequestViewModel = operationRequestListPage.BindingContext as OperationRequestViewModel;
                var content = operationRequestViewModel?
                    .Operations.FirstOrDefault(x => 
                        x.Amount.Equals(Operation.Amount) && 
                        x.Destination.Equals(Operation.Destination) &&
                        x.Source.Equals(Operation.Source));

                if (content != null)
                {
                    var indexOf = operationRequestViewModel.Operations.IndexOf(content);
                    operationRequestViewModel.Operations[indexOf] = Operation;
                }
            }
        }

        private async Task ClosePopup()
        {
            await Navigation.PopAsync();
        }
    }
}