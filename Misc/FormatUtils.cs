namespace Ultimate_Gshop.Misc
{
    public static class FormatUtils
    {
        public static DateTime ToDate(int unixTimestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
        }
    }
}
