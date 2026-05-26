namespace Web.Models;

public record GuildMember(DiscordUser? User, string? Nick, List<string> Roles, string? JoinedAt)
{
  public string DisplayName => Nick ?? User?.GlobalName ?? User?.Username ?? "(unknown)";
}
