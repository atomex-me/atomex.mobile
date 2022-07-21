using atomex.ViewModels.CurrencyViewModels;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.TezosTokens
{
    public partial class TokenActionBottomSheet : PopupPage
    {
        public TokenActionBottomSheet(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is TezosTokenViewModel)
            {
                var tezosTokenViewModel = (TezosTokenViewModel)BindingContext;
                if (tezosTokenViewModel.CloseActionSheetCommand.CanExecute(null))
                    tezosTokenViewModel.CloseActionSheetCommand.Execute(null);
            }
        }
    }
}
