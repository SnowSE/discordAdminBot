namespace Web.Models;

public record DiscordInvite(string Code, string? InviterId, string GuildId, string ChannelId)
{
  public string Url => $"https://discord.gg/{Code}";
}
