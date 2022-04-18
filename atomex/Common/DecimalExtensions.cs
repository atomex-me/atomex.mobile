namespace atomex.Common
{
    public static class DecimalExtensions
    {
        public static decimal SafeMultiply(this decimal arg1, decimal arg2, decimal overflowValue = 0)
        {
            try
            {
                return arg1 * arg2;
            }
            catch
            {
                return overflowValue;
            }
        }
    }
}
