namespace Web.Models;

public record GuildRole(
  DiscordRoleId Id,
  string Name,
  int Color,
  int Position,
  bool Managed,
  bool Mentionable
);
