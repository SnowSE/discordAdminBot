namespace Web.Models;

public record GuildChannelPermissionOverwrite(
  string Id,
  PermissionOverwriteType Type,
  long Allow,
  long Deny
);

public enum PermissionOverwriteType
{
  Role = 0,
  Member = 1,
}
