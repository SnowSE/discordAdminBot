using Microsoft.Data.Sqlite;

namespace Web.Services;

public record DbSession(SqliteConnection Connection, SqliteTransaction? Transaction);

public sealed class DbScope : IAsyncDisposable
{
  private bool _disposed;
  private bool _committed;

  public DbSession Session { get; }

  public DbScope(DbSession session) => Session = session;

  public async Task CommitAsync()
  {
    if (Session.Transaction is null)
      throw new InvalidOperationException("Cannot commit a session with no active transaction.");
    if (_committed)
      throw new InvalidOperationException("Transaction was already committed on this DbScope.");
    _committed = true;
    await Session.Transaction.CommitAsync();
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed)
      return;
    _disposed = true;

    if (Session.Transaction is not null && !_committed)
    {
      if (Session.Transaction.Connection?.State != System.Data.ConnectionState.Closed)
        await Session.Transaction.RollbackAsync();
      await Session.Transaction.DisposeAsync();
    }
    await Session.Connection.DisposeAsync();
  }
}
