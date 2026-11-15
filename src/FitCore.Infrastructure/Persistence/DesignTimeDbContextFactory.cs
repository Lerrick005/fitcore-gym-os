using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitCore.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FitCoreDbContext>
{
    public FitCoreDbContext CreateDbContext(string[] args)
    {
        // Point at the SAME database the running app uses
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FitCore");

        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var dbPath = Path.Combine(dataDir, "fitcore.db");

        var options = new DbContextOptionsBuilder<FitCoreDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;

        return new FitCoreDbContext(options);
    }
}
