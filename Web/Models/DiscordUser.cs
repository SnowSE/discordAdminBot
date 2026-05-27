namespace Web.Models;

public record DiscordUser(
  DiscordUserId Id,
  DiscordUsername Username,
  DiscordGlobalName? GlobalName,
  DiscordAvatar? Avatar,
  bool Bot
);
