using System;
namespace Adeptus.Models;

public class Issue
{
    public required int Id { get; set; }

    public required string Title { get; set; }

    public required bool IsDone { get; set; }

    public required DateTime Updated {  get; set; }
}
