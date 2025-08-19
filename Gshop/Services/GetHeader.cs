namespace Ultimate_Gshop.Gshop.Services
{
    internal static class GetHeader
    {
        public static (DateTime, int) Handle(BinaryReader br)
        {
            DateTime timestamp;
            int item_amount;

            timestamp = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(br.ReadInt32());
            item_amount = br.ReadInt32();

            return (timestamp, item_amount);
        }
    }
}