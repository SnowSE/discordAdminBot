using Web.Models.Snow;

namespace Web.Models;

public record CourseChannelAssignment(
  SnowCrn Crn,
  SnowTermCode TermCode,
  DiscordChannelId DiscordChannelId,
  DiscordRoleId DiscordRoleId,
  DateTime CreatedAt
);
