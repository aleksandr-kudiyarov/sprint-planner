namespace SprintPlanner.Application.Configuration;

/// <summary>
/// Global tunable parameters (F6.2). Persisted as a singleton row and injected into
/// statistics and planning. Defaults match the requirements.
/// </summary>
public sealed class GlobalSettings
{
    /// <summary>Well-known id of the single settings row.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-0000000005e7");

    public Guid Id { get; set; } = SingletonId;

    /// <summary>Default sprint length in working days.</summary>
    public int DefaultSprintLengthDays { get; set; } = 10;

    /// <summary>Effective work-time coefficient (e.g. 0.8 = 80% of time on tasks).</summary>
    public decimal EffectiveWorkCoefficient { get; set; } = 0.8m;

    /// <summary>Number of recent sprints used for rolling velocity (N).</summary>
    public int VelocityWindowSprints { get; set; } = 5;

    /// <summary>Load fraction above which the UI warns about overload (e.g. 0.85).</summary>
    public decimal LoadWarningThreshold { get; set; } = 0.85m;

    /// <summary>
    /// Minimum number of completed sprints before statistics are considered reliable.
    /// Below this, the UI shows the "insufficient data" badge (F2).
    /// </summary>
    public int MinSprintsForReliableStats { get; set; } = 3;

    // ----- Cold-start defaults (ADR Q4) -----

    /// <summary>β (estimate-accuracy factor) used when a developer has no history.</summary>
    public decimal ColdStartBeta { get; set; } = 1.0m;

    /// <summary>σ as a fraction of the estimate when a task type has no history.</summary>
    public decimal ColdStartSigmaFraction { get; set; } = 0.3m;
}
