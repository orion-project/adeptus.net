using System;
namespace Adeptus.Models;

public class Issue
{
    public required int Id { get; set; }

    public required string Summary { get; set; }

    public required DateTime Updated {  get; set; }
}
