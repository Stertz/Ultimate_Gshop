using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

public sealed class DateConversionTests
{
    [Theory]
    [InlineData(-1, 1969, 12, 31, 23, 59, 59)]
    [InlineData(0, 1970, 1, 1, 0, 0, 0)]
    [InlineData(1704067200, 2024, 1, 1, 0, 0, 0)]
    public void UnixSecondsKeepTheExistingDateTimeContract(
        int timestamp, int year, int month, int day, int hour, int minute, int second)
    {
        var result = FormatUtils.ToDate(timestamp);

        Assert.Equal(new DateTime(year, month, day, hour, minute, second), result);
        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
    }
}
