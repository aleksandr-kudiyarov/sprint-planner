namespace SprintPlanner.Domain.ValueObjects;

/// <summary>
/// Normalized weights for the five optimization criteria. The components always sum
/// to 1.0 (see <see cref="Normalize"/>). Maps directly to the objective:
/// <c>max w1·BusinessValue + w2·LoadBalance + w3·SkillFit + w4·(−Risk) + w5·Continuity</c>.
/// </summary>
public sealed class OptimizationWeights : IEquatable<OptimizationWeights>
{
    public decimal BusinessValue { get; }
    public decimal LoadBalance { get; }
    public decimal SkillFit { get; }
    public decimal RiskMinimization { get; }
    public decimal Continuity { get; }

    public OptimizationWeights(
        decimal businessValue,
        decimal loadBalance,
        decimal skillFit,
        decimal riskMinimization,
        decimal continuity)
    {
        if (businessValue < 0 || loadBalance < 0 || skillFit < 0 || riskMinimization < 0 || continuity < 0)
        {
            throw new ArgumentException("Optimization weights must be non-negative.");
        }

        var sum = businessValue + loadBalance + skillFit + riskMinimization + continuity;
        if (sum <= 0)
        {
            throw new ArgumentException("At least one optimization weight must be positive.");
        }

        BusinessValue = businessValue / sum;
        LoadBalance = loadBalance / sum;
        SkillFit = skillFit / sum;
        RiskMinimization = riskMinimization / sum;
        Continuity = continuity / sum;
    }

    /// <summary>Convenience factory; identical to the constructor but reads as intent.</summary>
    public static OptimizationWeights Normalize(
        decimal businessValue, decimal loadBalance, decimal skillFit, decimal riskMinimization, decimal continuity)
        => new(businessValue, loadBalance, skillFit, riskMinimization, continuity);

    // Preset profiles (F6.1).
    public static OptimizationWeights Value => new(0.5m, 0.1m, 0.2m, 0.1m, 0.1m);
    public static OptimizationWeights Stability => new(0.2m, 0.2m, 0.2m, 0.3m, 0.1m);
    public static OptimizationWeights Balance => new(0.2m, 0.3m, 0.2m, 0.2m, 0.1m);

    public bool Equals(OptimizationWeights? other)
        => other is not null
           && BusinessValue == other.BusinessValue
           && LoadBalance == other.LoadBalance
           && SkillFit == other.SkillFit
           && RiskMinimization == other.RiskMinimization
           && Continuity == other.Continuity;

    public override bool Equals(object? obj) => Equals(obj as OptimizationWeights);

    public override int GetHashCode()
        => HashCode.Combine(BusinessValue, LoadBalance, SkillFit, RiskMinimization, Continuity);
}
