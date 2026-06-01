using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Web.Models;
using Web.Models.Snow;

namespace Web.Services;

public class DiscordDB(CacheDb cache)
{
  private readonly CacheDb _cache = cache;

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
  };

  public async Task<List<GuildMember>> GetMembersAsync()
  {
    await using var scope = _cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<string>(
      "SELECT data FROM discord_members",
      transaction: scope.Session.Transaction
    );
    return [.. rows.Select(json => JsonSerializer.Deserialize<GuildMember>(json, JsonOptions)!)];
  }

  public async Task<List<GuildChannel>> GetChannelsAsync()
  {
    await using var scope = _cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<string>(
      "SELECT data FROM discord_channels",
      transaction: scope.Session.Transaction
    );
    return [.. rows.Select(json => JsonSerializer.Deserialize<GuildChannel>(json, JsonOptions)!)];
  }

  public async Task<List<GuildRole>> GetRolesAsync()
  {
    await using var scope = _cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<string>(
      "SELECT data FROM discord_roles",
      transaction: scope.Session.Transaction
    );
    return [.. rows.Select(json => JsonSerializer.Deserialize<GuildRole>(json, JsonOptions)!)];
  }

  public async Task<DiscordGuild?> GetGuildAsync()
  {
    await using var scope = _cache.OpenSession();
    var json = await scope.Session.Connection.QuerySingleOrDefaultAsync<string>(
      "SELECT data FROM discord_guilds LIMIT 1",
      transaction: scope.Session.Transaction
    );
    return json is null ? null : JsonSerializer.Deserialize<DiscordGuild>(json, JsonOptions);
  }

  public async Task<DiscordUser?> GetBotUserAsync()
  {
    await using var scope = _cache.OpenSession();
    var json = await scope.Session.Connection.QuerySingleOrDefaultAsync<string>(
      "SELECT data FROM discord_bot_users LIMIT 1",
      transaction: scope.Session.Transaction
    );
    return json is null ? null : JsonSerializer.Deserialize<DiscordUser>(json, JsonOptions);
  }

  public async Task SaveMembersAsync(List<GuildMember> members)
  {
    await using var scope = await _cache.BeginTransactionAsync();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var now = DateTime.UtcNow.ToString("O");
    var rows = members
      .Select(m => new
      {
        id = m.User?.Id?.Value ?? "",
        data = JsonSerializer.Serialize(m, JsonOptions),
        updatedAt = now,
      })
      .ToList();

    await conn.ExecuteAsync(
      """
      INSERT INTO discord_members (id, data, updated_at) VALUES (@id, @data, @updatedAt)
      ON CONFLICT(id) DO UPDATE SET data = excluded.data, updated_at = excluded.updated_at
      """,
      rows,
      transaction: tx
    );

    if (rows.Count == 0)
    {
      await conn.ExecuteAsync("DELETE FROM discord_members", transaction: tx);
    }
    else
    {
      var keptIds = JsonSerializer.Serialize(rows.Select(r => r.id));
      await conn.ExecuteAsync(
        "DELETE FROM discord_members WHERE id NOT IN (SELECT value FROM json_each(@ids))",
        new { ids = keptIds },
        transaction: tx
      );
    }

    await scope.CommitAsync();
  }

  public async Task SaveChannelsAsync(List<GuildChannel> channels)
  {
    await using var scope = await _cache.BeginTransactionAsync();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var now = DateTime.UtcNow.ToString("O");
    var rows = channels
      .Select(c => new
      {
        id = c.Id,
        data = JsonSerializer.Serialize(c, JsonOptions),
        updatedAt = now,
      })
      .ToList();

    await conn.ExecuteAsync(
      """
      INSERT INTO discord_channels (id, data, updated_at) VALUES (@id, @data, @updatedAt)
      ON CONFLICT(id) DO UPDATE SET data = excluded.data, updated_at = excluded.updated_at
      """,
      rows,
      transaction: tx
    );

    if (rows.Count == 0)
    {
      await conn.ExecuteAsync("DELETE FROM discord_channels", transaction: tx);
    }
    else
    {
      var keptIds = JsonSerializer.Serialize(rows.Select(r => r.id.Value));
      await conn.ExecuteAsync(
        "DELETE FROM discord_channels WHERE id NOT IN (SELECT value FROM json_each(@ids))",
        new { ids = keptIds },
        transaction: tx
      );
    }

    await scope.CommitAsync();
  }

  public async Task SaveRolesAsync(List<GuildRole> roles)
  {
    await using var scope = await _cache.BeginTransactionAsync();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var now = DateTime.UtcNow.ToString("O");
    var rows = roles
      .Select(r => new
      {
        id = r.Id,
        data = JsonSerializer.Serialize(r, JsonOptions),
        updatedAt = now,
      })
      .ToList();

    await conn.ExecuteAsync(
      """
      INSERT INTO discord_roles (id, data, updated_at) VALUES (@id, @data, @updatedAt)
      ON CONFLICT(id) DO UPDATE SET data = excluded.data, updated_at = excluded.updated_at
      """,
      rows,
      transaction: tx
    );

    if (rows.Count == 0)
    {
      await conn.ExecuteAsync("DELETE FROM discord_roles", transaction: tx);
    }
    else
    {
      var keptIds = JsonSerializer.Serialize(rows.Select(r => r.id.Value));
      await conn.ExecuteAsync(
        "DELETE FROM discord_roles WHERE id NOT IN (SELECT value FROM json_each(@ids))",
        new { ids = keptIds },
        transaction: tx
      );
    }

    await scope.CommitAsync();
  }

  public async Task SaveGuildAsync(DiscordGuild guild)
  {
    await using var scope = await _cache.BeginTransactionAsync();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var now = DateTime.UtcNow.ToString("O");
    await conn.ExecuteAsync("DELETE FROM discord_guilds", transaction: tx);
    await conn.ExecuteAsync(
      "INSERT INTO discord_guilds (id, data, updated_at) VALUES (@id, @data, @updatedAt)",
      new
      {
        id = guild.Id,
        data = JsonSerializer.Serialize(guild, JsonOptions),
        updatedAt = now,
      },
      transaction: tx
    );
    await scope.CommitAsync();
  }

  public async Task SaveBotUserAsync(DiscordUser botUser)
  {
    await using var scope = await _cache.BeginTransactionAsync();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;

    var now = DateTime.UtcNow.ToString("O");
    await conn.ExecuteAsync("DELETE FROM discord_bot_users", transaction: tx);
    await conn.ExecuteAsync(
      "INSERT INTO discord_bot_users (id, data, updated_at) VALUES (@id, @data, @updatedAt)",
      new
      {
        id = botUser.Id,
        data = JsonSerializer.Serialize(botUser, JsonOptions),
        updatedAt = now,
      },
      transaction: tx
    );
    await scope.CommitAsync();
  }

  public async Task<DateTime?> GetLastSyncedAtAsync()
  {
    await using var scope = _cache.OpenSession();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;
    var membersTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_members",
      transaction: tx
    );
    var channelsTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_channels",
      transaction: tx
    );
    var rolesTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_roles",
      transaction: tx
    );
    var guildTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_guilds",
      transaction: tx
    );
    var botUserTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_bot_users",
      transaction: tx
    );
    await Task.WhenAll(membersTsTask, channelsTsTask, rolesTsTask, guildTsTask, botUserTsTask);
    var timestamps = new[]
    {
      membersTsTask.Result,
      channelsTsTask.Result,
      rolesTsTask.Result,
      guildTsTask.Result,
      botUserTsTask.Result,
    }
      .Where(s => s is not null)
      .Select(s => DateTime.Parse(s!, null, System.Globalization.DateTimeStyles.RoundtripKind))
      .Cast<DateTime?>()
      .Max();

    return timestamps;
  }

  public async Task<List<DiscordInvite>> GetSharesAsync()
  {
    await using var scope = _cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<string>(
      "SELECT data FROM discord_shares",
      transaction: scope.Session.Transaction
    );
    return rows.Select(json => JsonSerializer.Deserialize<DiscordInvite>(json, JsonOptions)!)
      .ToList();
  }

  public async Task SaveSharesAsync(List<DiscordInvite> shares)
  {
    await using var scope = _cache.OpenSession();
    var conn = scope.Session.Connection;
    var tx = scope.Session.Transaction;
    var now = DateTime.UtcNow.ToString("O");
    await conn.ExecuteAsync("DELETE FROM discord_shares", transaction: tx);
    var rows = shares
      .Select(s => new
      {
        id = s.Code,
        data = JsonSerializer.Serialize(s, JsonOptions),
        updatedAt = now,
      })
      .ToList();
    await conn.ExecuteAsync(
      "INSERT INTO discord_shares (id, data, updated_at) VALUES (@id, @data, @updatedAt)",
      rows,
      transaction: tx
    );
  }

  public async Task<List<CourseChannelAssignment>> GetCourseChannelAssignmentsAsync()
  {
    await using var scope = _cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<(
      string crn,
      string termCode,
      string channelId,
      string roleId,
      string createdAt
    )>(
      "SELECT crn, term_code, discord_channel_id, discord_role_id, created_at FROM course_channel_assignments",
      transaction: scope.Session.Transaction
    );
    return
    [
      .. rows.Select(r => new CourseChannelAssignment(
        new SnowCrn(r.crn),
        new SnowTermCode(r.termCode),
        new DiscordChannelId(r.channelId),
        new DiscordRoleId(r.roleId),
        DateTime.Parse(r.createdAt, null, System.Globalization.DateTimeStyles.RoundtripKind)
      )),
    ];
  }

  public async Task SaveCourseChannelAssignmentAsync(CourseChannelAssignment assignment)
  {
    await using var scope = _cache.OpenSession();
    await scope.Session.Connection.ExecuteAsync(
      """
      INSERT INTO course_channel_assignments (crn, term_code, discord_channel_id, discord_role_id)
      VALUES (@crn, @termCode, @channelId, @roleId)
      ON CONFLICT(crn) DO UPDATE SET
        term_code = excluded.term_code,
        discord_channel_id = excluded.discord_channel_id,
        discord_role_id = excluded.discord_role_id
      """,
      new
      {
        crn = assignment.Crn.Value,
        termCode = assignment.TermCode.Value,
        channelId = assignment.DiscordChannelId.Value,
        roleId = assignment.DiscordRoleId.Value,
      },
      transaction: scope.Session.Transaction
    );
  }

  public async Task SaveStudentDiscordMappingAsync(
    SnowBadgerId badgerId,
    DiscordUserId discordUserId
  )
  {
    await using var scope = _cache.OpenSession();
    await scope.Session.Connection.ExecuteAsync(
      """
      INSERT INTO student_discord_mapping (badger_id, discord_user_id)
      VALUES (@badgerId, @discordUserId)
      ON CONFLICT(badger_id) DO UPDATE SET discord_user_id = excluded.discord_user_id
      """,
      new { badgerId = badgerId.Value, discordUserId = discordUserId.Value },
      transaction: scope.Session.Transaction
    );
  }

  public async Task DeleteStudentDiscordMappingAsync(SnowBadgerId badgerId)
  {
    await using var scope = _cache.OpenSession();
    await scope.Session.Connection.ExecuteAsync(
      "DELETE FROM student_discord_mapping WHERE badger_id = @badgerId",
      new { badgerId = badgerId.Value },
      transaction: scope.Session.Transaction
    );
  }

  public async Task<
    List<(SnowBadgerId BadgerId, DiscordUserId DiscordUserId)>
  > GetStudentDiscordMappingsAsync()
  {
    await using var scope = _cache.OpenSession();
    var rows = await scope.Session.Connection.QueryAsync<(string badgerId, string discordUserId)>(
      "SELECT badger_id, discord_user_id FROM student_discord_mapping ORDER BY badger_id",
      transaction: scope.Session.Transaction
    );
    return
    [
      .. rows.Select(r => (new SnowBadgerId(r.badgerId), new DiscordUserId(r.discordUserId))),
    ];
  }

  public async Task<SnowBadgerId?> GetMappedBadgerIdAsync(DiscordUserId discordUserId)
  {
    await using var scope = _cache.OpenSession();
    var badgerIdStr = await scope.Session.Connection.QuerySingleOrDefaultAsync<string?>(
      "SELECT badger_id FROM student_discord_mapping WHERE discord_user_id = @discordUserId",
      new { discordUserId = discordUserId.Value },
      transaction: scope.Session.Transaction
    );
    return badgerIdStr is null ? null : new SnowBadgerId(badgerIdStr);
  }

  public async Task DeleteOrphanedCourseChannelAssignmentsAsync()
  {
    await using var scope = _cache.OpenSession();
    await scope.Session.Connection.ExecuteAsync(
      """
      DELETE FROM course_channel_assignments
      WHERE discord_channel_id NOT IN (SELECT id FROM discord_channels)
         OR discord_role_id NOT IN (SELECT id FROM discord_roles)
      """,
      transaction: scope.Session.Transaction
    );
  }

  public async Task DeleteCourseChannelAssignmentAsync(SnowCrn crn)
  {
    await using var scope = _cache.OpenSession();
    await scope.Session.Connection.ExecuteAsync(
      "DELETE FROM course_channel_assignments WHERE crn = @crn",
      new { crn = crn.Value },
      transaction: scope.Session.Transaction
    );
  }
}
