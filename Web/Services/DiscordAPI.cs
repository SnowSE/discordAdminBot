using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web.Models;

namespace Web.Services;

public class DiscordAPI(IHttpClientFactory httpClientFactory, AppConfig config)
{
  private readonly HttpClient _http = CreateHttpClient(httpClientFactory, config);
  private readonly string _guildId = config.DiscordGuildId;

  private static HttpClient CreateHttpClient(IHttpClientFactory httpClientFactory, AppConfig config)
  {
    var client = httpClientFactory.CreateClient();
    client.BaseAddress = new Uri("https://discord.com/api/v10/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
      "Bot",
      config.DiscordBotToken
    );
    client.DefaultRequestHeaders.Add("User-Agent", "DiscordAdminBot/1.0 (ASP.NET Core)");
    return client;
  }

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
  };

  public async Task<DiscordGuild> FetchGuildAsync(CancellationToken ct = default) =>
    await _http.GetFromJsonAsync<DiscordGuild>($"guilds/{_guildId}", JsonOptions, ct)
    ?? throw new InvalidOperationException(
      $"Discord guild response was empty while loading guild '{_guildId}'."
    );

  public async Task<DiscordUser> FetchCurrentUserAsync(CancellationToken ct = default) =>
    await _http.GetFromJsonAsync<DiscordUser>("users/@me", JsonOptions, ct)
    ?? throw new InvalidOperationException(
      "Discord current user response was empty while loading bot identity."
    );

  public async Task<List<GuildMember>> FetchMembersAsync(CancellationToken ct = default)
  {
    var allMembers = new List<GuildMember>();
    var lastMemberId = "0";
    while (true)
    {
      var members = await _http.GetFromJsonAsync<List<GuildMember>>(
        $"guilds/{_guildId}/members?limit=1000&after={lastMemberId}",
        JsonOptions,
        ct
      );

      if (members is null || members.Count == 0)
      {
        break;
      }

      allMembers.AddRange(members);
      var lastMember = members.Last();
      if (lastMember.User is null)
      {
        break;
      }
      lastMemberId = lastMember.User.Id.Value;
    }
    return allMembers;
  }

  public async Task<List<GuildChannel>> FetchChannelsAsync(CancellationToken ct = default) =>
    (
      await _http.GetFromJsonAsync<List<GuildChannel>>(
        $"guilds/{_guildId}/channels",
        JsonOptions,
        ct
      )
    ) ?? [];

  public async Task<List<GuildRole>> FetchRolesAsync(CancellationToken ct = default) =>
    (await _http.GetFromJsonAsync<List<GuildRole>>($"guilds/{_guildId}/roles", JsonOptions, ct))
    ?? [];

  public async Task<List<DiscordInvite>> FetchInvitesAsync(CancellationToken ct = default) =>
    (
      await _http.GetFromJsonAsync<List<DiscordInvite>>(
        $"guilds/{_guildId}/invites",
        JsonOptions,
        ct
      )
    ) ?? [];

  public async Task<DiscordInvite> CreateInviteAsync(
    string channelId,
    CancellationToken ct = default
  )
  {
    var response = await _http.PostAsJsonAsync(
      $"channels/{channelId}/invites",
      new
      {
        max_uses = 0,
        max_age = 0,
        temporary = false,
      },
      JsonOptions,
      ct
    );
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<DiscordInvite>(JsonOptions, ct))!;
  }

  public async Task DeleteInviteAsync(string inviteCode, CancellationToken ct = default)
  {
    var response = await _http.DeleteAsync($"invites/{inviteCode}", ct);
    response.EnsureSuccessStatusCode();
  }

  private const long ViewChannelBit = 1L << 10;
  private const long SendMessagesBit = 1L << 11;
  private const long AddReactionsBit = 1L << 6;
  private const long EmbedLinksBit = 1L << 14;
  private const long AttachFilesBit = 1L << 15;
  private const long ReadMessageHistoryBit = 1L << 16;
  private const long UseExternalEmojisBit = 1L << 18;
  private const long CreatePublicThreadsBit = 1L << 35;
  private const long SendMessagesInThreadsBit = 1L << 38;

  public async Task<GuildChannel> CreateCategoryAsync(
    string name,
    bool isPrivate = false,
    DiscordRoleId? roleId = null,
    CancellationToken ct = default
  )
  {
    var permissionOverwrites = BuildPermissionOverwrites(isPrivate, roleId);

    var response = await _http.PostAsJsonAsync(
      $"guilds/{_guildId}/channels",
      new
      {
        name,
        type = 4,
        permission_overwrites = permissionOverwrites,
      },
      JsonOptions,
      ct
    );
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<GuildChannel>(JsonOptions, ct))!;
  }

  private List<object> BuildPermissionOverwrites(bool isPrivate, DiscordRoleId? roleId)
  {
    if (!isPrivate || roleId is null)
      return [];

    // var studentPermissions =
    //   ViewChannelBit
    //   | SendMessagesBit
    //   | ReadMessageHistoryBit
    //   | AddReactionsBit
    //   | EmbedLinksBit
    //   | AttachFilesBit
    //   | UseExternalEmojisBit;

    return
    [
      new
      {
        id = _guildId,
        type = PermissionOverwriteType.Role,
        allow = "0",
        deny = ViewChannelBit.ToString(),
      },
      new
      {
        id = roleId.Value,
        type = PermissionOverwriteType.Role,
        allow = ViewChannelBit.ToString(),
        deny = "0",
      },
    ];
  }

  public async Task<GuildChannel> CreateTextChannelAsync(
    string name,
    string parentId,
    bool isPrivate,
    DiscordRoleId? roleId = null,
    bool parentHasOverwrites = true,
    CancellationToken ct = default
  )
  {
    var permissionOverwrites = parentHasOverwrites
      ? []
      : BuildPermissionOverwrites(isPrivate, roleId);

    var response = await _http.PostAsJsonAsync(
      $"guilds/{_guildId}/channels",
      new
      {
        name,
        type = 0,
        parent_id = parentId,
        permission_overwrites = permissionOverwrites,
      },
      JsonOptions,
      ct
    );
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<GuildChannel>(JsonOptions, ct))!;
  }

  public async Task<GuildChannel> CreateVoiceChannelAsync(
    string name,
    string parentId,
    CancellationToken ct = default
  )
  {
    var response = await _http.PostAsJsonAsync(
      $"guilds/{_guildId}/channels",
      new
      {
        name,
        type = 2,
        parent_id = parentId,
      },
      JsonOptions,
      ct
    );
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<GuildChannel>(JsonOptions, ct))!;
  }

  public async Task DeleteChannelAsync(string channelId, CancellationToken ct = default)
  {
    var response = await _http.DeleteAsync($"channels/{channelId}", ct);
    response.EnsureSuccessStatusCode();
  }

  public async Task RenameChannelAsync(
    string channelId,
    string name,
    CancellationToken ct = default
  )
  {
    var request = new HttpRequestMessage(
      HttpMethod.Patch,
      new Uri(_http.BaseAddress!, $"channels/{channelId}")
    );
    request.Content = JsonContent.Create(new { name }, options: JsonOptions);
    request.Headers.Add("X-Audit-Log-Reason", "Course name updated from discord admin bot");
    var response = await _http.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();
  }

  public async Task<GuildRole> CreateRoleAsync(
    string roleName,
    int color,
    bool mentionable = false,
    CancellationToken ct = default
  )
  {
    var response = await _http.PostAsJsonAsync(
      $"guilds/{_guildId}/roles",
      new
      {
        name = roleName,
        color,
        mentionable,
      },
      JsonOptions,
      ct
    );
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<GuildRole>(JsonOptions, ct))!;
  }

  public async Task DeleteRoleAsync(string roleId, CancellationToken ct = default)
  {
    var response = await _http.DeleteAsync($"guilds/{_guildId}/roles/{roleId}", ct);
    response.EnsureSuccessStatusCode();
  }

  public async Task AddRoleToMemberAsync(
    string memberId,
    string roleId,
    CancellationToken ct = default
  )
  {
    var response = await _http.PutAsync(
      $"guilds/{_guildId}/members/{memberId}/roles/{roleId}",
      null,
      ct
    );
    response.EnsureSuccessStatusCode();
  }

  public async Task RemoveRoleFromMemberAsync(
    string memberId,
    string roleId,
    CancellationToken ct = default
  )
  {
    var response = await _http.DeleteAsync(
      $"guilds/{_guildId}/members/{memberId}/roles/{roleId}",
      ct
    );
    response.EnsureSuccessStatusCode();
  }
}
