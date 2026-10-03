using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Services;

// Case-level risk, derived from the per-finding rule scores (Teammate 3's weighted-sum scores).
// Independent findings compound - 1 - product of (1 - score) - so two medium findings outrank one,
// and a case with no scored findings is 0. Computed on read rather than stored, so it can never
// drift from the findings it summarizes; replace with the rules engine's own case score once it
// produces one.
public static class CaseRisk
{
    public const double HighThreshold = 0.8;
    public const double MediumThreshold = 0.4;

    public static double Score(IEnumerable<double?> findingScores) =>
        1 - findingScores.Aggregate(1.0, (acc, s) => acc * (1 - Math.Clamp(s ?? 0, 0, 1)));

    public static FindingSeverity Tier(double score) =>
        score >= HighThreshold ? FindingSeverity.High
        : score >= MediumThreshold ? FindingSeverity.Medium
        : FindingSeverity.Low;
}
