
namespace atomex.ViewModel
{
    public class BakerViewModel : BaseViewModel
    {
        public string Logo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Fee { get; set; }
        public decimal Roi { get; set; }
        public decimal MinDelegation { get; set; }
        public decimal StakingAvailable { get; set; }
        public bool IsCurrentlyActive { get; set; }

        public bool IsFull => StakingAvailable <= 0;
        public bool IsMinDelegation => MinDelegation > 0;

        public string FreeSpaceFormatted => StakingAvailable.ToString(StakingAvailable switch
        {
            > 999999999 => "0,,,.#B",
            > 999999 => "0,,.#M",
            > 999 => "0,.#K",
            < -999 => "0,.#K",
            _ => "0"
        });
    }
}

