namespace Web.Models;

public record GuildMember(
  DiscordUser? User,
  GuildNick? Nick,
  List<DiscordRoleId> Roles,
  GuildJoinedAt? JoinedAt
)
{
  public string DisplayName =>
    Nick?.Value ?? User?.GlobalName?.Value ?? User?.Username?.Value ?? "(unknown)";
}
