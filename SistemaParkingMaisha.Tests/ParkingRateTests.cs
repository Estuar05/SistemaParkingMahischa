using SistemaParkingMahischa.Services;
using Xunit;

namespace SistemaParkingMaisha.Tests;

public sealed class ParkingRateTests
{
    private static readonly DateTime EntryAt = new(2026, 9, 7, 8, 0, 0);

    [Theory]
    [InlineData(0, 700)]
    [InlineData(59, 700)]
    [InlineData(60, 700)]
    [InlineData(61, 900)]
    [InlineData(70, 900)]
    [InlineData(71, 1000)]
    [InlineData(80, 1000)]
    [InlineData(81, 1100)]
    [InlineData(90, 1100)]
    [InlineData(91, 1200)]
    [InlineData(100, 1200)]
    [InlineData(101, 1300)]
    [InlineData(110, 1300)]
    [InlineData(111, 1400)]
    [InlineData(120, 1400)]
    [InlineData(121, 1600)]
    [InlineData(240, 2800)]
    [InlineData(241, 3000)]
    [InlineData(719, 3000)]
    [InlineData(720, 3000)]
    public void HourlyRateUsesExpectedTenMinuteTiersAndCap(int elapsedMinutes, decimal expected)
    {
        var amount = ParkingService.CalculateAmount(
            EntryAt,
            EntryAt.AddMinutes(elapsedMinutes),
            "Hora",
            700m,
            graceMinutes: 0,
            blockMinutes: 720,
            blockAmount: 3000m);

        Assert.Equal(expected, amount);
    }

    [Fact]
    public void IncompleteSecondsDoNotAdvanceToNextTier()
    {
        var amount = ParkingService.CalculateAmount(
            EntryAt,
            EntryAt.AddMinutes(70).AddSeconds(59),
            "Hora",
            700m,
            graceMinutes: 0,
            blockMinutes: 720,
            blockAmount: 3000m);

        Assert.Equal(900m, amount);
    }

    [Fact]
    public void ReusingOpeningTimestampKeepsQuotedAmountFrozen()
    {
        var quotedAt = EntryAt.AddMinutes(70);

        var shownAmount = ParkingService.CalculateAmount(
            EntryAt, quotedAt, "Hora", 700m, 0, 720, 3000m);
        var persistedAmount = ParkingService.CalculateAmount(
            EntryAt, quotedAt, "Hora", 700m, 0, 720, 3000m);

        Assert.Equal(900m, shownAmount);
        Assert.Equal(shownAmount, persistedAmount);
    }
}
