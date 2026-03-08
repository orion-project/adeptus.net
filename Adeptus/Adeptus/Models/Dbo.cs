using System;

namespace Adeptus.Models;

public class IssueDbo
{
    /// <summary>
    /// Issue identifier (auto-generated)
    /// </summary>
    public required int Id { get; set; }

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
    public required DateTime Created { get; set; }

    /// <summary>
    /// A date when the issue was updated
    /// </summary>
    public required DateTime Updated { get; set; }

    /// <summary>
    /// A workplace where the issue was created, e.g. "home", "work"
    /// </summary>
    public required string Place { get; set; }

    /// <summary>
    /// If the issue is completed (solved, closed)
    /// </summary>
    public required bool IsDone { get; set; }
}
