using Web.Models;

namespace Web.Services;

public class DiscordService(DiscordAPI api, DiscordDB db)
{
  public static event Func<Task>? DbDataChanged;

  public async Task SyncAllAsync(CancellationToken ct = default)
  {
    var membersTask = SyncMembersAsync(skipNotify: true, ct: ct);
    var channelsTask = SyncChannelsAsync(skipNotify: true, ct: ct);
    var rolesTask = SyncRolesAsync(skipNotify: true, ct: ct);
    var invitesTask = SyncInvitesAsync(skipNotify: true, ct: ct);

    await Task.WhenAll(membersTask, channelsTask, rolesTask, invitesTask);
    DbDataChanged?.Invoke();
  }

  public async Task SyncMembersAsync(bool skipNotify = false, CancellationToken ct = default)
  {
    var members = await api.FetchMembersAsync(ct);
    var roleAssignments = members
      .SelectMany(m => m.Roles.Select(roleId => (roleId, memberId: m.User?.Id ?? "")))
      .ToList();

    await db.SaveMembersAsync(members);
    await db.SaveRoleAssignmentsAsync(roleAssignments);
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
    var roles = await db.GetRolesAsync();
    var members = await db.GetMembersAsync();
    var roleMemberMap = await db.GetRoleAssignmentsAsync();

    var memberMap = members.ToDictionary(m => m.User?.Id ?? "", m => m);

    return roles
      .Where(r => r.Name != "@everyone")
      .OrderByDescending(r => r.Position)
      .Select(role =>
      {
        var assignedMemberIds = roleMemberMap.GetValueOrDefault(role.Id, []);
        var assignedMembers = assignedMemberIds
          .Select(memberId => memberMap.GetValueOrDefault(memberId))
          .OfType<GuildMember>()
          .ToList();
        return new RoleAssignment(role, assignedMembers);
      })
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
