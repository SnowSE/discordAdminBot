using Dapper;
using Microsoft.Data.Sqlite;

namespace Web.Services;

/// <summary>
/// Opens and initialises the SQLite cache database.
/// Registered as a singleton; call OpenConnection() to get a connection.
/// </summary>
public class CacheDb
{
  private readonly string _connectionString;

  public CacheDb(AppConfig config)
  {
    _connectionString = $"Data Source={config.CacheDbPath}";
    InitialiseSchema();
  }

  public SqliteConnection OpenConnection() => new(_connectionString);

  private void InitialiseSchema()
  {
    using var conn = OpenConnection();
    conn.Open();
    conn.Execute(
      """
      CREATE TABLE IF NOT EXISTS discord_members (
          id         TEXT    NOT NULL PRIMARY KEY,
          data       TEXT    NOT NULL,
          updated_at TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
      );

      CREATE TABLE IF NOT EXISTS discord_channels (
          id         TEXT    NOT NULL PRIMARY KEY,
          data       TEXT    NOT NULL,
          updated_at TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
      );

      CREATE TABLE IF NOT EXISTS discord_roles (
          id         TEXT    NOT NULL PRIMARY KEY,
          data       TEXT    NOT NULL,
          updated_at TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
      );
      """
    );
  }
}
