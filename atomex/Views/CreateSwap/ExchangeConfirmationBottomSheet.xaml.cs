using atomex.ViewModel;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.CreateSwap
{
    public partial class ExchangeConfirmationBottomSheet : PopupPage
    {
        public ExchangeConfirmationBottomSheet(ConversionConfirmationViewModel conversionConfirmationViewModel)
        {
            InitializeComponent();
            BindingContext = conversionConfirmationViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is ConversionConfirmationViewModel)
            {
                var sendViewModel = (ConversionConfirmationViewModel)BindingContext;
                if (sendViewModel.UndoConfirmStageCommand.CanExecute(null))
                    sendViewModel.UndoConfirmStageCommand.Execute(null);
            }
        }
    }
}
