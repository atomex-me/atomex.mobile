namespace atomex.Models
{
    public class AuthToken
    {
        public string Id { get; set; }
        public string Token { get; set; }
        public decimal Expires { get; set; }
    }
}
