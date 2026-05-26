using Web.Models;

namespace Web.Services;

public class DiscordService(DiscordAPI api, DiscordDB db)
{
  public static event Func<Task>? DbDataChanged;

  public async Task SyncAsync(CancellationToken ct = default)
  {
    var membersTask = api.FetchMembersAsync(ct);
    var channelsTask = api.FetchChannelsAsync(ct);
    var rolesTask = api.FetchRolesAsync(ct);

    await Task.WhenAll(membersTask, channelsTask, rolesTask);

    await db.SaveMembersAsync(membersTask.Result);
    await db.SaveChannelsAsync(channelsTask.Result);
    await db.SaveRolesAsync(rolesTask.Result);
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

    return roles
      .Where(r => r.Name != "@everyone")
      .OrderByDescending(r => r.Position)
      .Select(role =>
      {
        var roleId = role.Id;
        var assignedMembers = members.Where(m => m.Roles.Contains(roleId)).ToList();
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
