using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.Data.Sqlite;
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
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<string>("SELECT data FROM discord_members");
    return [.. rows.Select(json => JsonSerializer.Deserialize<GuildMember>(json, JsonOptions)!)];
  }

  public async Task<List<GuildChannel>> GetChannelsAsync()
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<string>("SELECT data FROM discord_channels");
    return [.. rows.Select(json => JsonSerializer.Deserialize<GuildChannel>(json, JsonOptions)!)];
  }

  public async Task<List<GuildRole>> GetRolesAsync()
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<string>("SELECT data FROM discord_roles");
    return [.. rows.Select(json => JsonSerializer.Deserialize<GuildRole>(json, JsonOptions)!)];
  }

  public async Task<DiscordGuild?> GetGuildAsync()
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var json = await conn.QuerySingleOrDefaultAsync<string>(
      "SELECT data FROM discord_guilds LIMIT 1"
    );
    return json is null ? null : JsonSerializer.Deserialize<DiscordGuild>(json, JsonOptions);
  }

  public async Task<DiscordUser?> GetBotUserAsync()
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var json = await conn.QuerySingleOrDefaultAsync<string>(
      "SELECT data FROM discord_bot_users LIMIT 1"
    );
    return json is null ? null : JsonSerializer.Deserialize<DiscordUser>(json, JsonOptions);
  }

  public async Task SaveMembersAsync(List<GuildMember> members)
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    try
    {
      var now = DateTime.UtcNow.ToString("O");
      await conn.ExecuteAsync("DELETE FROM discord_members", transaction: tx);
      var rows = members
        .Select(m => new
        {
          id = m.User?.Id ?? "",
          data = JsonSerializer.Serialize(m, JsonOptions),
          updatedAt = now,
        })
        .ToList();
      await conn.ExecuteAsync(
        "INSERT INTO discord_members (id, data, updated_at) VALUES (@id, @data, @updatedAt)",
        rows,
        transaction: tx
      );
      await tx.CommitAsync();
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
  }

  public async Task SaveChannelsAsync(List<GuildChannel> channels)
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    try
    {
      var now = DateTime.UtcNow.ToString("O");
      await conn.ExecuteAsync("DELETE FROM discord_channels", transaction: tx);
      var rows = channels
        .Select(c => new
        {
          id = c.Id,
          data = JsonSerializer.Serialize(c, JsonOptions),
          updatedAt = now,
        })
        .ToList();
      await conn.ExecuteAsync(
        "INSERT INTO discord_channels (id, data, updated_at) VALUES (@id, @data, @updatedAt)",
        rows,
        transaction: tx
      );
      await tx.CommitAsync();
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
  }

  public async Task SaveRolesAsync(List<GuildRole> roles)
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    try
    {
      var now = DateTime.UtcNow.ToString("O");
      await conn.ExecuteAsync("DELETE FROM discord_roles", transaction: tx);
      var rows = roles
        .Select(r => new
        {
          id = r.Id,
          data = JsonSerializer.Serialize(r, JsonOptions),
          updatedAt = now,
        })
        .ToList();
      await conn.ExecuteAsync(
        "INSERT INTO discord_roles (id, data, updated_at) VALUES (@id, @data, @updatedAt)",
        rows,
        transaction: tx
      );
      await tx.CommitAsync();
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
  }

  public async Task SaveGuildAsync(DiscordGuild guild)
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    try
    {
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
      await tx.CommitAsync();
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
  }

  public async Task SaveBotUserAsync(DiscordUser botUser)
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    try
    {
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
      await tx.CommitAsync();
    }
    catch
    {
      await tx.RollbackAsync();
      throw;
    }
  }

  public async Task<DateTime?> GetLastSyncedAtAsync()
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var membersTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_members"
    );
    var channelsTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_channels"
    );
    var rolesTsTask = conn.ExecuteScalarAsync<string?>("SELECT MAX(updated_at) FROM discord_roles");
    var guildTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_guilds"
    );
    var botUserTsTask = conn.ExecuteScalarAsync<string?>(
      "SELECT MAX(updated_at) FROM discord_bot_users"
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
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<string>("SELECT data FROM discord_shares");
    return rows.Select(json => JsonSerializer.Deserialize<DiscordInvite>(json, JsonOptions)!)
      .ToList();
  }

  public async Task SaveSharesAsync(List<DiscordInvite> shares)
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var now = DateTime.UtcNow.ToString("O");
    await conn.ExecuteAsync("DELETE FROM discord_shares");
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
      rows
    );
  }

  public async Task<List<CourseChannelAssignment>> GetCourseChannelAssignmentsAsync()
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<(
      string crn,
      string termCode,
      string channelId,
      string roleId,
      string createdAt
    )>(
      "SELECT crn, term_code, discord_channel_id, discord_role_id, created_at FROM course_channel_assignments"
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
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    await conn.ExecuteAsync(
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
      }
    );
  }

  public async Task SaveStudentDiscordMappingAsync(
    SnowBadgerId badgerId,
    DiscordUserId discordUserId
  )
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    await conn.ExecuteAsync(
      """
      INSERT INTO student_discord_mapping (badger_id, discord_user_id)
      VALUES (@badgerId, @discordUserId)
      ON CONFLICT(badger_id) DO UPDATE SET discord_user_id = excluded.discord_user_id
      """,
      new { badgerId = badgerId.Value, discordUserId = discordUserId.Value }
    );
  }

  public async Task DeleteStudentDiscordMappingAsync(SnowBadgerId badgerId)
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    await conn.ExecuteAsync(
      "DELETE FROM student_discord_mapping WHERE badger_id = @badgerId",
      new { badgerId = badgerId.Value }
    );
  }

  public async Task<
    List<(SnowBadgerId BadgerId, DiscordUserId DiscordUserId)>
  > GetStudentDiscordMappingsAsync()
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var rows = await conn.QueryAsync<(string badgerId, string discordUserId)>(
      "SELECT badger_id, discord_user_id FROM student_discord_mapping ORDER BY badger_id"
    );
    return
    [
      .. rows.Select(r => (new SnowBadgerId(r.badgerId), new DiscordUserId(r.discordUserId))),
    ];
  }

  public async Task<SnowBadgerId?> GetMappedBadgerIdAsync(DiscordUserId discordUserId)
  {
    using var conn = _cache.OpenConnection();
    await conn.OpenAsync();
    var badgerIdStr = await conn.QuerySingleOrDefaultAsync<string?>(
      "SELECT badger_id FROM student_discord_mapping WHERE discord_user_id = @discordUserId",
      new { discordUserId = discordUserId.Value }
    );
    return badgerIdStr is null ? null : new SnowBadgerId(badgerIdStr);
  }
}
