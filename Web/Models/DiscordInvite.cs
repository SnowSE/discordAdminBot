namespace Web.Models;

public record DiscordInvite(
  InviteCode Code,
  DiscordUserId? InviterId,
  DiscordGuildId GuildId,
  PartialChannel? Channel
)
{
  public string Url => $"https://discord.gg/{Code}";
}

public record PartialChannel(DiscordChannelId Id, ChannelName? Name, int Type);
