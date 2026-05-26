namespace Web;

public record AppConfig(string DiscordBotToken, string DiscordGuildId, string CacheDbPath)
{
  public static AppConfig FromConfiguration(IConfiguration config) =>
    new(
      DiscordBotToken: config["DISCORD_BOT_TOKEN"]
        ?? throw new InvalidOperationException("DISCORD_BOT_TOKEN is not configured."),
      DiscordGuildId: config["DISCORD_GUILD_ID"]
        ?? throw new InvalidOperationException("DISCORD_GUILD_ID is not configured."),
      CacheDbPath: config["CACHE_DB_PATH"] ?? "discord_cache.db"
    );
}
