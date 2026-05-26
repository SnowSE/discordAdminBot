namespace Web.Models;

public record GuildRole(
  string Id,
  string Name,
  int Color,
  int Position,
  bool Managed,
  bool Mentionable
);
