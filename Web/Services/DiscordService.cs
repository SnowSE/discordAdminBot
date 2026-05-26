using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Web.Models;

namespace Web.Services;

public class DiscordService(IHttpClientFactory httpClientFactory, AppConfig config, CacheDb cache)
{
  private readonly HttpClient _http = httpClientFactory.CreateClient("discord");
  private readonly CacheDb _cache = cache;
  private readonly string _guildId = config.DiscordGuildId;

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
  };

  // ── Manual sync ───────────────────────────────────────────────────────────

  /// <summary>Force-fetches all data from Discord and overwrites the cache.</summary>
  public async Task SyncAsync(CancellationToken ct = default)
  {
    var membersTask = FetchMembersFromDiscordAsync(ct);
    var channelsTask = _http.GetFromJsonAsync<List<GuildChannel>>(
      $"guilds/{_guildId}/channels",
      JsonOptions,
      ct
    );
    var rolesTask = _http.GetFromJsonAsync<List<GuildRole>>(
      $"guilds/{_guildId}/roles",
      JsonOptions,
      ct
    );

    await Task.WhenAll(membersTask, channelsTask, rolesTask);

    using var conn = _cache.OpenConnection();
    conn.Open();
    WriteCache(conn, "discord_members", membersTask.Result, m => m.User?.Id ?? "");
    WriteCache(conn, "discord_channels", channelsTask.Result ?? [], c => c.Id);
    WriteCache(conn, "discord_roles", rolesTask.Result ?? [], r => r.Id);
  }

  // ── Members ──────────────────────────────────────────────────────────────

  public Task<List<GuildMember>> GetMembersAsync(CancellationToken ct = default)
  {
    using var conn = _cache.OpenConnection();
    conn.Open();
    return Task.FromResult(ReadCache<GuildMember>(conn, "discord_members"));
  }

  private async Task<List<GuildMember>> FetchMembersFromDiscordAsync(CancellationToken ct)
  {
    var members = new List<GuildMember>();
    string? after = null;

    while (true)
    {
      var url =
        $"guilds/{_guildId}/members?limit=1000{(after is not null ? $"&after={after}" : "")}";
      var page = await _http.GetFromJsonAsync<List<GuildMember>>(url, JsonOptions, ct) ?? [];
      members.AddRange(page);

      if (page.Count < 1000)
        break;

      after = page[^1].User?.Id;
    }

    return members;
  }

  // ── Channels ─────────────────────────────────────────────────────────────

  public Task<List<GuildChannel>> GetChannelsAsync(CancellationToken ct = default)
  {
    using var conn = _cache.OpenConnection();
    conn.Open();
    return Task.FromResult(ReadCache<GuildChannel>(conn, "discord_channels"));
  }

  // ── Roles ─────────────────────────────────────────────────────────────────

  public Task<List<GuildRole>> GetRolesAsync(CancellationToken ct = default)
  {
    using var conn = _cache.OpenConnection();
    conn.Open();
    return Task.FromResult(ReadCache<GuildRole>(conn, "discord_roles"));
  }

  // ── Last sync timestamp ───────────────────────────────────────────────────

  /// <summary>Returns the most recent updated_at across all three tables, or null if cache is empty.</summary>
  public DateTime? GetLastSyncedAt()
  {
    using var conn = _cache.OpenConnection();
    conn.Open();
    var latest = new[]
    {
      conn.ExecuteScalar<string?>($"SELECT MAX(updated_at) FROM discord_members"),
      conn.ExecuteScalar<string?>($"SELECT MAX(updated_at) FROM discord_channels"),
      conn.ExecuteScalar<string?>($"SELECT MAX(updated_at) FROM discord_roles"),
    }
      .Where(s => s is not null)
      .Select(s => DateTime.Parse(s!, null, System.Globalization.DateTimeStyles.RoundtripKind))
      .Cast<DateTime?>()
      .Max();

    return latest;
  }

  // ── Cache helpers ─────────────────────────────────────────────────────────

  private static List<T> ReadCache<T>(Microsoft.Data.Sqlite.SqliteConnection conn, string table)
  {
    var rows = conn.Query<string>($"SELECT data FROM {table}");
    return rows.Select(json => JsonSerializer.Deserialize<T>(json, JsonOptions)!).ToList();
  }

  private static void WriteCache<T>(
    Microsoft.Data.Sqlite.SqliteConnection conn,
    string table,
    List<T> items,
    Func<T, string> idSelector
  )
  {
    var now = DateTime.UtcNow.ToString("O");
    conn.Execute($"DELETE FROM {table}");
    foreach (var item in items)
    {
      conn.Execute(
        $"INSERT INTO {table} (id, data, updated_at) VALUES (@id, @data, @updatedAt)",
        new
        {
          id = idSelector(item),
          data = JsonSerializer.Serialize(item, JsonOptions),
          updatedAt = now,
        }
      );
    }
  }
}
