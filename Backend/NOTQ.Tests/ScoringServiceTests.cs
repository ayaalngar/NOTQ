using FluentAssertions;
using NOTQ.Domain.Enums;
using NOTQ.Infrastructure.Services;
using Xunit;

namespace NOTQ.Tests;

public class ScoringServiceTests
{
    private readonly ScoringService _scoring = new();

    [Theory]
    [InlineData(10, 10, 1.0)]
    [InlineData(7, 10, 0.70)]
    [InlineData(0, 10, 0.0)]
    [InlineData(5, 8, 0.63)]
    public void CalculateSessionScore_ShouldReturnExpectedProportion(int correct, int total, double expected)
    {
        var score = _scoring.CalculateSessionScore(correct, total);
        score.Should().Be(expected);
    }

    [Fact]
    public void CalculateSessionScore_WithZeroTotalAttempts_ShouldReturnZeroWithoutError()
    {
        var score = _scoring.CalculateSessionScore(0, 0);
        score.Should().Be(0.0);
    }

    [Fact]
    public void DetermineTrend_WithImprovingScores_ShouldReturnImproving()
    {
        var scores = new List<double> { 0.50, 0.60, 0.85 };
        var trend = _scoring.DetermineTrend(scores);
        trend.Should().Be(SessionTrend.Improving);
    }

    [Fact]
    public void DetermineTrend_WithDecliningScores_ShouldReturnDeclining()
    {
        var scores = new List<double> { 0.85, 0.80, 0.60 };
        var trend = _scoring.DetermineTrend(scores);
        trend.Should().Be(SessionTrend.Declining);
    }

    [Fact]
    public void DetermineTrend_WithStableScores_ShouldReturnStable()
    {
        var scores = new List<double> { 0.70, 0.72, 0.71 };
        var trend = _scoring.DetermineTrend(scores);
        trend.Should().Be(SessionTrend.Stable);
    }

    [Fact]
    public void DetermineTrend_WithSingleScore_ShouldReturnInsufficientData()
    {
        var scores = new List<double> { 0.80 };
        var trend = _scoring.DetermineTrend(scores);
        trend.Should().Be(SessionTrend.InsufficientData);
    }
}
