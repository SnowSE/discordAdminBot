namespace Web.Models;

public record GuildMember(
  DiscordUser? User,
  GuildNick? Nick,
  List<DiscordRoleId> Roles,
  GuildJoinedAt? JoinedAt
)
{
  public string DisplayName =>
    ValueAsNonEmptyString(Nick)
    ?? ValueAsNonEmptyString(User?.GlobalName)
    ?? ValueAsNonEmptyString(User?.Username)
    ?? "(unknown)";

  private static string? ValueAsNonEmptyString(TypedString? value)
  {
    var s = (string?)value;
    return string.IsNullOrWhiteSpace(s) ? null : s;
  }
}
