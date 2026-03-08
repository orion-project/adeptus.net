using Microsoft.EntityFrameworkCore;

namespace Adeptus.Models;

public class AppDbContext(string filePath) : DbContext()
{
    public DbSet<IssueDbo> Issues { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //string dbPath = Path.Combine(AppContext.BaseDirectory, fileName);
        optionsBuilder.UseSqlite($"Data Source={filePath}");
    }

    public static void Migrate(string fileName)
    {
        using var db = new AppDbContext(fileName);
        db.Database.Migrate();
    }
}
