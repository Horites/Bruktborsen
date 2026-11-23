using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services;

public static class AnalyticsMath
{
    public static decimal GetConditionFactor(Condition condition)
    {
        return condition switch
        {
            Condition.New => 1.00m,
            Condition.LikeNew => 0.85m,
            Condition.Good => 0.70m,
            Condition.Used => 0.55m,
            Condition.Defect => 0.30m,

            _ => throw new ArgumentOutOfRangeException(
                nameof(condition),
                condition,
                "Unknown item condition.")
        };
    }

    public static decimal NormalizePrice(
        decimal soldPrice,
        Condition condition)
    {
        return soldPrice / GetConditionFactor(condition);
    }

    public static decimal Median(IEnumerable<decimal> values)
    {
        var sorted = values.OrderBy(x => x).ToArray();

        if (sorted.Length == 0)
        {
            throw new ArgumentException(
                "At least one value is required.",
                nameof(values));
        }

        int middle = sorted.Length / 2;

        if (sorted.Length % 2 == 1)
        {
            return sorted[middle];
        }

        return (sorted[middle - 1] + sorted[middle]) / 2m;
    }

    // Inclusive percentile, equivalent to Excel PERCENTILE.INC.
    public static decimal PercentileInc(
        IEnumerable<decimal> values,
        decimal percentile)
    {
        if (percentile < 0m || percentile > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(percentile),
                "Percentile must be between 0 and 1.");
        }

        var sorted = values.OrderBy(x => x).ToArray();

        if (sorted.Length == 0)
        {
            throw new ArgumentException(
                "At least one value is required.",
                nameof(values));
        }

        if (sorted.Length == 1)
        {
            return sorted[0];
        }

        decimal position = (sorted.Length - 1) * percentile;

        int lower = (int)decimal.Floor(position);
        int upper = (int)decimal.Ceiling(position);

        if (lower == upper)
        {
            return sorted[lower];
        }

        decimal fraction = position - lower;

        return sorted[lower] +
            (sorted[upper] - sorted[lower]) * fraction;
    }

    public static decimal Quartile1(IEnumerable<decimal> values)
    {
        return PercentileInc(values, 0.25m);
    }

    public static decimal Quartile3(IEnumerable<decimal> values)
    {
        return PercentileInc(values, 0.75m);
    }

    public static decimal SampleStandardDeviation(
        IEnumerable<decimal> values)
    {
        var data = values.ToArray();

        if (data.Length < 2)
        {
            throw new ArgumentException(
                "At least two values are required.",
                nameof(values));
        }

        decimal mean = data.Average();

        decimal sumSquaredDifferences = data.Sum(
            x => (x - mean) * (x - mean));

        double variance = (double)(
            sumSquaredDifferences / (data.Length - 1));

        return (decimal)Math.Sqrt(variance);
    }

    public static decimal RoundToNearestTen(decimal value)
    {
        return Math.Round(
            value / 10m,
            0,
            MidpointRounding.AwayFromZero) * 10m;
    }

    public static string AssessPrice(
        decimal listingPrice,
        IEnumerable<decimal> comparableSoldPrices)
    {
        var prices = comparableSoldPrices.ToArray();

        if (prices.Length < 5)
        {
            return "Unknown";
        }

        decimal q1 = Quartile1(prices);
        decimal q3 = Quartile3(prices);
        decimal iqr = q3 - q1;

        decimal lowerFence = q1 - 1.5m * iqr;
        decimal upperFence = q3 + 1.5m * iqr;

        if (listingPrice < lowerFence)
        {
            return "Low";
        }

        if (listingPrice > upperFence)
        {
            return "High";
        }

        return "Normal";
    }
}
