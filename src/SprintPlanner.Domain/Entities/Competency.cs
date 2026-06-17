using SprintPlanner.Domain.Common;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// A team competency / domain, e.g. "Kafka", "PostgreSQL", "API", "Auth".
/// </summary>
public class Competency : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
