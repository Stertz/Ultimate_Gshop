using System.Globalization;

namespace Ultimate_Gshop.Misc;

internal static class PriceUtils
{
    internal static decimal Parse(string? text) =>
        decimal.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Number,
            CultureInfo.InvariantCulture, out var value) ? value : 0m;

    internal static int ToCents(decimal value)
    {
        // Clamp before multiplying, including decimal.MaxValue and decimal.MinValue.
        if (value <= 0m) return 0;
        if (value >= int.MaxValue / 100m) return int.MaxValue;
        return RoundCents(value * 100m);
    }

    internal static int RoundCents(decimal cents) =>
        (int)Math.Round(Math.Clamp(cents, 0m, int.MaxValue), 0, MidpointRounding.AwayFromZero);

    internal static bool TryApply(int currentCents, string? operation, decimal value, out int result)
    {
        decimal current = Math.Max(currentCents, 0);
        result = currentCents;
        if (operation == "Divide" && value == 0m) return true;
        try
        {
            switch (operation)
            {
                case "Price": result = ToCents(value); break;
                case "Add %": result = RoundCents(current * (1m + value / 100m)); break;
                case "Remove %": result = RoundCents(current * (1m - value / 100m)); break;
                case "Multiply": result = RoundCents(current * value); break;
                case "Divide": result = RoundCents(current / value); break;
                default: return false;
            }
        }
        catch (OverflowException)
        {
            // Decimal arithmetic can overflow before the final integer clamp.
            var positive = operation == "Remove %" ? value < 0m : value > 0m;
            result = positive ? int.MaxValue : 0;
        }
        return true;
    }
}
