using System.Globalization;
using Atomex;
using Xamarin.Forms;
using ReactiveUI;

namespace atomex.ViewModels.SendViewModels
{
    public class NftSendViewModel : TezosTokensSendViewModel
    {
        public override string AmountString
        {
            get => Amount.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(
                        s: temp,
                        style: NumberStyles.AllowDecimalPoint,
                        provider: CultureInfo.InvariantCulture,
                        result: out var amount))
                {
                    Amount = 0;
                }
                else
                {
                    Amount = (int)amount;

                    if (Amount > long.MaxValue)
                        Amount = long.MaxValue;
                }
                SetAmountFromString(Amount.ToString());
                
                Device.InvokeOnMainThreadAsync(() =>
                {
                    this.RaisePropertyChanged(nameof(Amount));
                    this.RaisePropertyChanged(nameof(AmountString));
                });
            }
        }
        
        public NftSendViewModel(
            IAtomexApp app,
            INavigationService navigationService,
            string tokenContract,
            int tokenId,
            string tokenType,
            UriImageSource tokenPreview,
            string from = null)
            : base(app, navigationService, tokenContract, tokenId, tokenType, tokenPreview, from)
        {
        }
    }
}