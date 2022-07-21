using atomex.ViewModels.CurrencyViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.TezosTokens
{
    public partial class ManageTokensBottomSheet : PopupPage
    {
        public ManageTokensBottomSheet(TezosTokensViewModel tezosTokensViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokensViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is TezosTokensViewModel)
            {
                var tezosTokensViewModel = (TezosTokensViewModel)BindingContext;
                if (tezosTokensViewModel.CloseActionSheetCommand.CanExecute(null))
                    tezosTokensViewModel.CloseActionSheetCommand.Execute(null);
            }
        }
    }
}
