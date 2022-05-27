namespace atomex.Common
{
    public static class AddressExtensions
    {
        public static string TruncateAddress(this string address)
        {
            if (string.IsNullOrEmpty(address))
                return address;

            return $"{address.Substring(0, 5)}...{address.Substring(address.Length - 5, 5)}";
        }
    }
}
