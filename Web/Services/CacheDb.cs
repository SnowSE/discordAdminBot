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

      CREATE TABLE IF NOT EXISTS discord_guilds (
          id         TEXT    NOT NULL PRIMARY KEY,
          data       TEXT    NOT NULL,
          updated_at TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
      );

        CREATE TABLE IF NOT EXISTS discord_bot_users (
          id         TEXT    NOT NULL PRIMARY KEY,
          data       TEXT    NOT NULL,
          updated_at TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
        );

      CREATE TABLE IF NOT EXISTS discord_shares (
          id         TEXT    NOT NULL PRIMARY KEY,
          data       TEXT    NOT NULL,
          updated_at TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
      );

      DROP TABLE IF EXISTS discord_role_assignments;

      CREATE TABLE IF NOT EXISTS snow_terms (
          term_code  TEXT NOT NULL PRIMARY KEY,
          name       TEXT NOT NULL,
          updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
      );

      CREATE TABLE IF NOT EXISTS snow_courses (
          id         INTEGER PRIMARY KEY AUTOINCREMENT,
          term_code  TEXT    NOT NULL REFERENCES snow_terms(term_code) ON DELETE CASCADE,
          data       TEXT    NOT NULL
      );

      DROP TABLE IF EXISTS snow_student_schedules;
      DROP TABLE IF EXISTS snow_student_classes;

      CREATE TABLE IF NOT EXISTS course_channel_assignments (
          crn                TEXT NOT NULL PRIMARY KEY,
          term_code          TEXT NOT NULL,
          discord_channel_id TEXT NOT NULL,
          discord_role_id    TEXT NOT NULL,
          created_at         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
      );

      CREATE TABLE IF NOT EXISTS snow_section_students (
          id              INTEGER PRIMARY KEY AUTOINCREMENT,
          crn             TEXT NOT NULL,
          term_code       TEXT NOT NULL,
          data            TEXT NOT NULL,
          last_synced_at  TEXT
      );

      CREATE TABLE IF NOT EXISTS student_discord_mapping (
          badger_id       TEXT NOT NULL PRIMARY KEY,
          discord_user_id TEXT NOT NULL UNIQUE
      );
      """
    );
  }
}
