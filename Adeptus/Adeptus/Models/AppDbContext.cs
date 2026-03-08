using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace Adeptus.Models;

public class AppDbContext : DbContext
{
    public DbSet<Issue> Issue { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(AppContext.BaseDirectory, "demo.bugs");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}
