using System;

namespace Adeptus.Models;

public class IssueDbo
{
    /// <summary>
    /// Issue identifier (auto-generated)
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Short issue description, one row without line breakes
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Detailed issue description in markdown format
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// A date when the issue was created
    /// </summary>
    public DateTime Created { get; set; } = DateTime.Now;

    /// <summary>
    /// A date when the issue was updated
    /// </summary>
    public DateTime Updated { get; set; } = DateTime.Now;

    /// <summary>
    /// A workplace where the issue was created, e.g. "home", "work"
    /// </summary>
    public string Place { get; set; } = string.Empty;

    /// <summary>
    /// If the issue is completed (solved, closed)
    /// </summary>
    public bool IsDone { get; set; } = false;
}
