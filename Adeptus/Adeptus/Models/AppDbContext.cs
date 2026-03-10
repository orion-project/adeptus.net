using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adeptus.Models;

public class AppDbContext(string filePath) : DbContext()
{
    private DbSet<IssueDbo> Issues { get; set; }

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

    public async Task<IEnumerable<Issue>> GetIssues()
    {
        var dbos = await Issues.AsNoTracking().ToListAsync();
        return dbos.Select(MakeIssueFromDbo);
    }

    public async Task<Issue> CreateIssue(IssueCreateData data)
    {
        var dbo = new IssueDbo()
        {
            Title = data.Title,
            Description = data.Description,
        };
        Issues.Add(dbo);
        await SaveChangesAsync();
        return MakeIssueFromDbo(dbo);
    }

    private static Issue MakeIssueFromDbo(IssueDbo dbo)
    {
        return new()
        {
            Id = dbo.Id ?? 0,
            Title = dbo.Title,
            IsDone = dbo.IsDone,
            Updated = dbo.Updated,
        };
    }
}
