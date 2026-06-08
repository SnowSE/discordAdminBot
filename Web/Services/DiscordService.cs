using Web.Models;
using Web.Models.Snow;

namespace Web.Services;

public class DiscordService(DiscordAPI api, DiscordDB db, SnowCourseService snow)
{
  public static event Func<Task>? DbDataChanged;
  public static event Func<Task>? StudentMappingChanged;

  public async Task SyncAllAsync(CancellationToken ct = default)
  {
    var guildTask = SyncGuildAsync(skipNotify: true, ct: ct);
    var botUserTask = SyncBotUserAsync(skipNotify: true, ct: ct);
    var membersTask = SyncMembersAsync(skipNotify: true, ct: ct);
    var channelsTask = SyncChannelsAsync(skipNotify: true, ct: ct);
    var rolesTask = SyncRolesAsync(skipNotify: true, ct: ct);
    var invitesTask = SyncInvitesAsync(skipNotify: true, ct: ct);

    await Task.WhenAll(guildTask, botUserTask, membersTask, channelsTask, rolesTask, invitesTask);
    await db.DeleteOrphanedCourseChannelAssignmentsAsync();
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
    await api.CreateInviteAsync(channelId, ct);
    await SyncInvitesAsync(false, ct);
    DbDataChanged?.Invoke();
  }

  public async Task DeleteShareAsync(string inviteCode, CancellationToken ct = default)
  {
    await api.DeleteInviteAsync(inviteCode, ct);
    await SyncInvitesAsync(false, ct);
    DbDataChanged?.Invoke();
  }

  public async Task<List<RoleAssignment>> GetRoleAssignmentsAsync()
  {
    var rolesTask = db.GetRolesAsync();
    var membersTask = db.GetMembersAsync();
    await Task.WhenAll(rolesTask, membersTask);

    var roles = rolesTask.Result;
    var members = membersTask.Result;

    return
    [
      .. roles
        .Where(role => role.Name != "@everyone")
        .OrderByDescending(role => role.Position)
        .Select(role => new RoleAssignment(
          role,
          members.Where(member => member.Roles.Contains(role.Id)).ToList()
        )),
    ];
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

  public async Task CreateSharingVoiceChannelAsync(
    DiscordChannelId categoryId,
    CancellationToken ct = default
  )
  {
    var channel = await api.CreateVoiceChannelAsync("sharing", categoryId.Value, ct);

    var freshChannels = await api.FetchChannelsAsync(ct);
    await db.SaveChannelsAsync(freshChannels);

    DbDataChanged?.Invoke();
  }

  public async Task RenameChannelAsync(
    DiscordChannelId channelId,
    string newName,
    CancellationToken ct = default
  )
  {
    await api.RenameChannelAsync(channelId.Value, newName, ct);

    var freshChannels = await api.FetchChannelsAsync(ct);
    await db.SaveChannelsAsync(freshChannels);

    DbDataChanged?.Invoke();
  }

  public async Task DeleteCourseChannelAsync(SnowCrn crn, CancellationToken ct = default)
  {
    var assignments = await GetCourseChannelAssignmentsAsync();
    var assignment = assignments.FirstOrDefault(a => a.Crn == crn);
    if (assignment is null)
      throw new InvalidOperationException(
        $"No course-channel assignment found for CRN '{crn}' while attempting to delete channel."
      );

    await api.DeleteChannelAsync(assignment.DiscordChannelId.Value, ct);
    await db.DeleteCourseChannelAssignmentAsync(crn);

    var freshChannels = await api.FetchChannelsAsync(ct);
    await db.SaveChannelsAsync(freshChannels);

    DbDataChanged?.Invoke();
  }

  public async Task<List<CourseChannelAssignment>> GetCourseChannelAssignmentsAsync()
  {
    return await db.GetCourseChannelAssignmentsAsync();
  }

  public async Task AssignCourseToChannelAsync(
    SnowCrn crn,
    SnowTermCode termCode,
    DiscordChannelId channelId,
    DiscordRoleId roleId,
    CancellationToken ct = default
  )
  {
    var assignment = new CourseChannelAssignment(crn, termCode, channelId, roleId, DateTime.UtcNow);
    await db.SaveCourseChannelAssignmentAsync(assignment);
    DbDataChanged?.Invoke();
  }

  public async Task<CourseChannelAssignment> SetupCourseChannelAsync(
    SnowCrn crn,
    SnowTermCode termCode,
    string channelName,
    DiscordChannelId? existingCategoryId,
    string? newCategoryName,
    DiscordRoleId roleId,
    bool isPrivate,
    CancellationToken ct = default
  )
  {
    if (existingCategoryId is null && string.IsNullOrWhiteSpace(newCategoryName))
      throw new ArgumentException(
        $"Either an existing category ID or a new category name must be provided when setting up channel for CRN '{crn}'."
      );

    var categoryId = existingCategoryId;
    var categoryHasOverwrites = false;

    if (categoryId is null)
    {
      if (string.IsNullOrWhiteSpace(newCategoryName))
        throw new InvalidOperationException(
          $"New category name was empty when attempting to create a category for CRN '{crn}'."
        );
      var newCategory = await api.CreateCategoryAsync(newCategoryName, isPrivate, roleId, ct);
      categoryId = newCategory.Id;
      categoryHasOverwrites = isPrivate;
    }
    else
    {
      var channels = await api.FetchChannelsAsync(ct);
      var category = channels.FirstOrDefault(c => c.Id.Value == categoryId.Value);
      categoryHasOverwrites =
        category?.PermissionOverwrites != null && category.PermissionOverwrites.Any();
    }

    var channel = await api.CreateTextChannelAsync(
      channelName,
      categoryId.Value,
      isPrivate,
      roleId,
      categoryHasOverwrites,
      ct
    );

    var freshChannels = await api.FetchChannelsAsync(ct);
    await db.SaveChannelsAsync(freshChannels);

    var assignment = new CourseChannelAssignment(
      crn,
      termCode,
      channel.Id,
      roleId,
      DateTime.UtcNow
    );
    await db.SaveCourseChannelAssignmentAsync(assignment);

    DbDataChanged?.Invoke();
    return assignment;
  }

  public async Task SyncCourseChannelAsync(
    SnowCrn crn,
    SnowTermCode termCode,
    string jwtToken,
    CancellationToken ct = default
  )
  {
    await snow.RefreshSectionStudentsAsync(termCode, crn, jwtToken, ct);

    var assignments = await GetCourseChannelAssignmentsAsync();
    var assignment =
      assignments.FirstOrDefault(a => a.Crn == crn)
      ?? throw new InvalidOperationException(
        $"No course-channel assignment found for CRN '{crn}' while syncing section students to Discord."
      );
    var cachedStudents = await snow.GetCachedSectionStudentsAsync(crn, termCode);
    var members = await GetMembersAsync();
    var mappings = await db.GetStudentDiscordMappingsAsync();

    foreach (var student in cachedStudents)
    {
      var mapping = mappings.FirstOrDefault(m => m.BadgerId == student.BadgerId);
      if (mapping == default((SnowBadgerId, DiscordUserId)))
        continue;

      var discordMember = members.FirstOrDefault(m => m.User?.Id == mapping.DiscordUserId);
      if (discordMember is null)
        continue;

      await api.AddRoleToMemberAsync(
        mapping.DiscordUserId.Value,
        assignment.DiscordRoleId.Value,
        ct
      );
    }

    DbDataChanged?.Invoke();
  }

  public Task<
    List<(SnowBadgerId BadgerId, DiscordUserId DiscordUserId)>
  > GetStudentDiscordMappingsAsync() => db.GetStudentDiscordMappingsAsync();

  public async Task SaveStudentDiscordMappingAsync(
    SnowBadgerId badgerId,
    DiscordUserId discordUserId
  )
  {
    await db.SaveStudentDiscordMappingAsync(badgerId, discordUserId);
    if (StudentMappingChanged is not null)
      await StudentMappingChanged();
  }

  public async Task DeleteStudentDiscordMappingAsync(SnowBadgerId badgerId)
  {
    await db.DeleteStudentDiscordMappingAsync(badgerId);
    if (StudentMappingChanged is not null)
      await StudentMappingChanged();
  }

  /// <summary>Formats a sync timestamp into a human-readable display string (e.g., "synced 2h ago").</summary>
  public static string? FormatSyncStatus(DateTime? lastSyncedAt)
  {
    if (lastSyncedAt is null)
      return null;

    var elapsed = DateTime.UtcNow - lastSyncedAt.Value;
    if (elapsed.TotalMinutes < 60)
      return $"synced {(int)elapsed.TotalMinutes}m ago";
    if (elapsed.TotalHours < 24)
      return $"synced {(int)elapsed.TotalHours}h ago";
    return $"synced {(int)elapsed.TotalDays}d ago";
  }
}
