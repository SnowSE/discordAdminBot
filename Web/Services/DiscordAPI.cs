using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web.Models;

namespace Web.Services;

public class DiscordAPI(IHttpClientFactory httpClientFactory, AppConfig config)
{
  private readonly HttpClient _http = httpClientFactory.CreateClient("discord");
  private readonly string _guildId = config.DiscordGuildId;

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
  };

  public async Task<List<GuildMember>> FetchMembersAsync(CancellationToken ct = default) =>
    await FetchMembersFromDiscordAsync(ct);

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

  public async Task<GuildRole> CreateRoleAsync(
    string roleName,
    int color = 0,
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

  private async Task<GuildMember> FetchMemberAsync(string memberId, CancellationToken ct)
  {
    return (
      await _http.GetFromJsonAsync<GuildMember>(
        $"guilds/{_guildId}/members/{memberId}",
        JsonOptions,
        ct
      )
    )!;
  }

  private async Task<List<GuildMember>> FetchMembersFromDiscordAsync(CancellationToken ct)
  {
    var members = new List<GuildMember>();
    string? after = null;
    var again = true;

    while (again)
    {
      var url =
        $"guilds/{_guildId}/members?limit=1000{(after is not null ? $"&after={after}" : "")}";
      var page = await _http.GetFromJsonAsync<List<GuildMember>>(url, JsonOptions, ct) ?? [];
      members.AddRange(page);

      if (page.Count < 1000)
        again = false;

      after = page[^1].User?.Id;
    }

    return members;
  }
}
