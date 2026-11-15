using System;
using System.IO;
using FitCore.Desktop;
using FitCore.Infrastructure;
using FitCore.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Tests;

/// <summary>
/// Shared helper: creates a fresh temp SQLite DB, applies migrations, builds a DI container.
/// Dispose disposes the provider and deletes the DB file.
/// </summary>
public sealed class TestHost : IDisposable
{
    public string DbPath { get; }
    public ServiceProvider Provider { get; }

    public TestHost()
    {
        DbPath = Path.Combine(Path.GetTempPath(), $"fitcore-testhost-{Guid.NewGuid():N}.db");

        var services = new ServiceCollection();
        services.AddFitCoreInfrastructure(DbPath);
        services.AddWizardViewModels();
        Provider = services.BuildServiceProvider();

        using var scope = Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FitCoreDbContext>();
        db.Database.Migrate();
    }

    public IServiceScope CreateScope() => Provider.CreateScope();

    public void Dispose()
    {
        Provider.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(DbPath)) File.Delete(DbPath);
    }
}

