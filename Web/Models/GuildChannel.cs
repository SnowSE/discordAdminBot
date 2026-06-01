namespace Web.Models;

public record GuildChannel(
  DiscordChannelId Id,
  ChannelName? Name,
  ChannelType Type,
  ChannelPosition? Position,
  DiscordChannelId? ParentId,
  List<GuildChannelPermissionOverwrite> PermissionOverwrites
)
{
  public string TypeName => Type.TypeName;
}
