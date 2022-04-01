using System.Threading.Tasks;
using System.Windows.Input;
using Atomex;
using Atomex.Core;
using Xamarin.Forms;

namespace atomex.ViewModel.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public bool IsStakingAvailable => CurrencyCode == "XTZ";

        public TezosCurrencyViewModel(
             IAtomexApp app, CurrencyConfig currency)
            : base(app, currency)
        {
        }

        private ICommand _stakingPageCommand;
        public ICommand StakingPageCommand => _stakingPageCommand ??= new Command(async () => await OnStakingButtonClicked());

        private async Task OnStakingButtonClicked()
        {
            await Navigation.PushAsync(new DelegationsListPage(new DelegateViewModel(_app, Navigation)));
        }
    }
}
