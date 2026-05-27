namespace Web.Models;

public record GuildMember(
  DiscordUser? User,
  string? Nick,
  List<DiscordRoleId> Roles,
  string? JoinedAt
)
{
  public string DisplayName => Nick ?? User?.GlobalName ?? User?.Username ?? "(unknown)";
}
