using Web.Models;

namespace Web.Services;

public class DiscordService(DiscordAPI api, DiscordDB db)
{
  public static event Func<Task>? DbDataChanged;

  public async Task SyncAllAsync(CancellationToken ct = default)
  {
    var guildTask = SyncGuildAsync(skipNotify: true, ct: ct);
    var botUserTask = SyncBotUserAsync(skipNotify: true, ct: ct);
    var membersTask = SyncMembersAsync(skipNotify: true, ct: ct);
    var channelsTask = SyncChannelsAsync(skipNotify: true, ct: ct);
    var rolesTask = SyncRolesAsync(skipNotify: true, ct: ct);
    var invitesTask = SyncInvitesAsync(skipNotify: true, ct: ct);

    await Task.WhenAll(guildTask, botUserTask, membersTask, channelsTask, rolesTask, invitesTask);
    DbDataChanged?.Invoke();
  }

  public async Task SyncGuildAsync(bool skipNotify = false, CancellationToken ct = default)
  {
    var guild = await api.FetchGuildAsync(ct);
    await db.SaveGuildAsync(guild);
    if (!skipNotify)
      DbDataChanged?.Invoke();
  }

  public async Task SyncBotUserAsync(bool skipNotify = false, CancellationToken ct = default)
  {
    var botUser = await api.FetchCurrentUserAsync(ct);
    await db.SaveBotUserAsync(botUser);
    if (!skipNotify)
      DbDataChanged?.Invoke();
  }

  public async Task SyncMembersAsync(bool skipNotify = false, CancellationToken ct = default)
  {
    var members = await api.FetchMembersAsync(ct);
    await db.SaveMembersAsync(members);
    if (!skipNotify)
      DbDataChanged?.Invoke();
  }

  public async Task SyncChannelsAsync(bool skipNotify = false, CancellationToken ct = default)
  {
    var channels = await api.FetchChannelsAsync(ct);
    await db.SaveChannelsAsync(channels);
    if (!skipNotify)
      DbDataChanged?.Invoke();
  }

  public async Task SyncRolesAsync(bool skipNotify = false, CancellationToken ct = default)
  {
    var roles = await api.FetchRolesAsync(ct);
    await db.SaveRolesAsync(roles);
    if (!skipNotify)
      DbDataChanged?.Invoke();
  }

  public async Task SyncInvitesAsync(bool skipNotify = false, CancellationToken ct = default)
  {
    var invites = await api.FetchInvitesAsync(ct);
    await db.SaveSharesAsync(invites);
    if (!skipNotify)
      DbDataChanged?.Invoke();
  }

  public Task<List<GuildMember>> GetMembersAsync() => db.GetMembersAsync();

  public Task<List<GuildChannel>> GetChannelsAsync() => db.GetChannelsAsync();

  public Task<List<GuildRole>> GetRolesAsync() => db.GetRolesAsync();

  public Task<DiscordGuild?> GetGuildAsync() => db.GetGuildAsync();

  public Task<DiscordUser?> GetBotUserAsync() => db.GetBotUserAsync();

  public Task<DateTime?> GetLastSyncedAtAsync() => db.GetLastSyncedAtAsync();

  public Task<List<DiscordInvite>> GetSharesAsync() => db.GetSharesAsync();

  public async Task GenerateShareAsync(string channelId, CancellationToken ct = default)
  {
    var shares = await db.GetSharesAsync();
    if (shares.Count > 0)
      return;

    var invite = await api.CreateInviteAsync(channelId, ct);
    await db.SaveSharesAsync([invite]);
    DbDataChanged?.Invoke();
  }

  public async Task<List<RoleAssignment>> GetRoleAssignmentsAsync()
  {
    var rolesTask = db.GetRolesAsync();
    var membersTask = db.GetMembersAsync();
    await Task.WhenAll(rolesTask, membersTask);

    var roles = rolesTask.Result;
    var members = membersTask.Result;

    return roles
      .Where(role => role.Name != "@everyone")
      .OrderByDescending(role => role.Position)
      .Select(role => new RoleAssignment(
        role,
        members.Where(member => member.Roles.Contains(role.Id)).ToList()
      ))
      .ToList();
  }

  public async Task<GuildRole> CreateRoleAsync(
    string name,
    int color = 0,
    bool mentionable = false,
    CancellationToken ct = default
  )
  {
    var role = await api.CreateRoleAsync(name, color, mentionable, ct);
    var roles = await api.FetchRolesAsync(ct);
    await db.SaveRolesAsync(roles);
    DbDataChanged?.Invoke();
    return role;
  }

  public async Task DeleteRoleAsync(string roleId, CancellationToken ct = default)
  {
    await api.DeleteRoleAsync(roleId, ct);
    var roles = await api.FetchRolesAsync(ct);
    await db.SaveRolesAsync(roles);
    DbDataChanged?.Invoke();
  }

  public async Task AddRoleToMemberAsync(
    string memberId,
    string roleId,
    CancellationToken ct = default
  )
  {
    await api.AddRoleToMemberAsync(memberId, roleId, ct);
    var members = await api.FetchMembersAsync(ct);
    await db.SaveMembersAsync(members);
    DbDataChanged?.Invoke();
  }

  public async Task RemoveRoleFromMemberAsync(
    string memberId,
    string roleId,
    CancellationToken ct = default
  )
  {
    await api.RemoveRoleFromMemberAsync(memberId, roleId, ct);
    var members = await api.FetchMembersAsync(ct);
    await db.SaveMembersAsync(members);
    DbDataChanged?.Invoke();
  }
}
