namespace Web.Models;

public record GuildRole(
  DiscordRoleId Id,
  RoleName Name,
  RoleColor Color,
  RolePosition Position,
  bool Managed,
  bool Mentionable
);
