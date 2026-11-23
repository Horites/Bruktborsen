using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Services;

namespace Bruktborsen.Tests.Analytics;

public class AnalyticsMathTests
{
    [Fact]
    public void Median_WithOddNumberOfValues_ReturnsMiddleValue()
    {
        decimal[] values = [100m, 300m, 200m];

        decimal result = AnalyticsMath.Median(values);

        Assert.Equal(200m, result);
    }

    [Fact]
    public void Median_WithEvenNumberOfValues_ReturnsAverageOfMiddleTwo()
    {
        decimal[] values = [100m, 200m, 300m, 400m];

        decimal result = AnalyticsMath.Median(values);

        Assert.Equal(250m, result);
    }

    [Fact]
    public void Quartiles_UseExcelInclusiveMethod()
    {
        decimal[] values =
        [
            10m, 20m, 30m, 40m,
            50m, 60m, 70m, 80m
        ];

        decimal q1 = AnalyticsMath.Quartile1(values);
        decimal q3 = AnalyticsMath.Quartile3(values);

        Assert.Equal(27.5m, q1);
        Assert.Equal(62.5m, q3);
    }

    [Fact]
    public void SampleStandardDeviation_UsesNMinusOne()
    {
        decimal[] values = [10m, 20m, 30m];

        decimal result =
            AnalyticsMath.SampleStandardDeviation(values);

        Assert.InRange(result, 9.999m, 10.001m);
    }

    [Theory]
    [InlineData(Condition.New, 1.00)]
    [InlineData(Condition.LikeNew, 0.85)]
    [InlineData(Condition.Good, 0.70)]
    [InlineData(Condition.Used, 0.55)]
    [InlineData(Condition.Defect, 0.30)]
    public void GetConditionFactor_ReturnsCorrectFactor(
        Condition condition,
        double expected)
    {
        decimal result = AnalyticsMath.GetConditionFactor(
            condition);

        Assert.Equal((decimal)expected, result);
    }

    [Fact]
    public void NormalizePrice_DividesByConditionFactor()
    {
        decimal result = AnalyticsMath.NormalizePrice(
            7000m,
            Condition.Good
        );

        Assert.Equal(10000m, result);
    }

    [Theory]
    [InlineData(124, 120)]
    [InlineData(125, 130)]
    [InlineData(126, 130)]
    public void RoundToNearestTen_RoundsCorrectly(
        int input,
        int expected)
    {
        decimal result = AnalyticsMath.RoundToNearestTen(
            input
        );

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AssessPrice_WithFewerThanFiveSales_ReturnsUnknown()
    {
        decimal[] prices = [100m, 110m, 120m, 130m];

        string result = AnalyticsMath.AssessPrice(
            500m,
            prices
        );

        Assert.Equal("Unknown", result);
    }

    [Theory]
    [InlineData(50, "Low")]
    [InlineData(120, "Normal")]
    [InlineData(200, "High")]
    public void AssessPrice_UsesIqrOutlierFences(
        int listingPrice,
        string expected)
    {
        decimal[] prices =
        [
            100m, 110m, 120m, 130m, 140m
        ];

        string result = AnalyticsMath.AssessPrice(
            listingPrice,
            prices
        );

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Median_WithNoValues_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => AnalyticsMath.Median([])
        );
    }

    [Fact]
    public void StandardDeviation_WithOneValue_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => AnalyticsMath.SampleStandardDeviation([100m])
        );
    }
}
