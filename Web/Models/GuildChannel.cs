namespace Web.Models;

public record GuildChannel(
  DiscordChannelId Id,
  ChannelName? Name,
  ChannelType Type,
  ChannelPosition? Position,
  DiscordChannelId? ParentId
)
{
  public string TypeName => Type.TypeName;
}
