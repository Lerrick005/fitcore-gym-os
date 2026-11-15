using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FitCore.Infrastructure.Persistence;

/// <summary>
/// Applies SQLite PRAGMAs on every opened connection (PRS §13.5).
/// foreign_keys=ON, journal_mode=WAL, busy_timeout=5000.
/// </summary>
public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyPragmas(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        ApplyPragmas(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void ApplyPragmas(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        cmd.ExecuteNonQuery();
    }
}
