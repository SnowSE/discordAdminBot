namespace Web.Models;

public record DiscordInvite(string Code, string? InviterId, string GuildId, PartialChannel? Channel)
{
  public string Url => $"https://discord.gg/{Code}";
  public string? ChannelId => Channel?.Id;
  public string? ChannelName => Channel?.Name;
}

public record PartialChannel(string Id, string? Name, int Type);
