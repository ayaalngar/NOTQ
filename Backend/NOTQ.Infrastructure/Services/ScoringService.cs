using NOTQ.Application.Interfaces;
using NOTQ.Domain.Enums;

namespace NOTQ.Infrastructure.Services;

public class ScoringService : IScoringService
{
    public double CalculateSessionScore(int correctAttempts, int totalAttempts)
    {
        if (totalAttempts <= 0)
        {
            return 0.0;
        }

        var score = (double)correctAttempts / totalAttempts;
        return Math.Round(Math.Clamp(score, 0.0, 1.0), 2, MidpointRounding.AwayFromZero);
    }

    public double CalculateConsistencyScore(IEnumerable<double> sessionScores)
    {
        var scoresList = sessionScores.ToList();
        if (scoresList.Count == 0)
        {
            return 0.0;
        }

        var average = scoresList.Average();
        if (scoresList.Count == 1)
        {
            return Math.Round(average, 2);
        }

        // Variance calculation
        var variance = scoresList.Average(s => Math.Pow(s - average, 2));
        var stdDev = Math.Sqrt(variance);

        // Lower variance = higher consistency score
        var consistency = Math.Clamp(1.0 - (stdDev * 1.5), 0.0, 1.0);
        return Math.Round(consistency, 2);
    }

    public SessionTrend DetermineTrend(IReadOnlyList<double> chronologicalScores)
    {
        if (chronologicalScores == null || chronologicalScores.Count < 2)
        {
            return SessionTrend.InsufficientData;
        }

        var latestScore = chronologicalScores[^1];
        var priorScores = chronologicalScores.Take(chronologicalScores.Count - 1).ToList();
        var priorAverage = priorScores.Average();

        var delta = latestScore - priorAverage;

        if (delta >= 0.08)
        {
            return SessionTrend.Improving;
        }

        if (delta <= -0.08)
        {
            return SessionTrend.Declining;
        }

        return SessionTrend.Stable;
    }
}
