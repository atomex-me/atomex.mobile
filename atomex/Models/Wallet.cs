namespace atomex.Models
{
    public class Wallet
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public float Amount { get; set; }
        public string ImageUrl { get; set; }
        public float Price { get; set; }
        public float Cost { get; set; }
        public float PercentInPortfolio { get; set; }
        public string Address { get; set; }
    }
}