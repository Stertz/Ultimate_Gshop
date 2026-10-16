using System.Globalization;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

public sealed class PriceEditingTests
{
    [Theory]
    [InlineData("12.345", 1235)]
    [InlineData(" 12,345 ", 1235)]
    [InlineData("0.005", 1)]
    [InlineData("0.004", 0)]
    [InlineData("-1", 0)]
    [InlineData("21474836.47", int.MaxValue)]
    [InlineData("21474836.475", int.MaxValue)]
    [InlineData("79228162514264337593543950335", int.MaxValue)]
    [InlineData("-79228162514264337593543950335", 0)]
    [InlineData("invalid", 0)]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    public void TextPricesHaveConsistentRoundingAndSafeIntegerLimits(string? text, int expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            Assert.Equal(expected, PriceUtils.ToCents(PriceUtils.Parse(text)));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("Price", "12.345", 1235)]
    [InlineData("Add %", "10", 1100)]
    [InlineData("Remove %", "10", 900)]
    [InlineData("Remove %", "150", 0)]
    [InlineData("Multiply", "1.2345", 1235)]
    [InlineData("Divide", "3", 333)]
    [InlineData("Divide", "0", 1000)]
    [InlineData("Multiply", "-5", 0)]
    [InlineData("Multiply", "79228162514264337593543950335", int.MaxValue)]
    [InlineData("Multiply", "-79228162514264337593543950335", 0)]
    [InlineData("Add %", "79228162514264337593543950335", int.MaxValue)]
    [InlineData("Remove %", "-79228162514264337593543950335", int.MaxValue)]
    [InlineData("Remove %", "79228162514264337593543950335", 0)]
    [InlineData("Divide", "0.0000000000000000000000000001", int.MaxValue)]
    [InlineData("Divide", "-0.0000000000000000000000000001", 0)]
    public void AdvancedPriceOperationsRoundAndClampWithoutOverflow(string operation, string value, int expected)
    {
        Assert.True(PriceUtils.TryApply(1000, operation, PriceUtils.Parse(value), out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Unknown")]
    public void UnknownOperationsLeaveThePriceUnchanged(string? operation)
    {
        Assert.False(PriceUtils.TryApply(1234, operation, 10, out var actual));
        Assert.Equal(1234, actual);
    }

    [Fact]
    public void ZeroPriceRemainsZeroWithAnExtremeMultiplier()
    {
        Assert.True(PriceUtils.TryApply(0, "Multiply", decimal.MaxValue, out var actual));
        Assert.Equal(0, actual);
    }
}
