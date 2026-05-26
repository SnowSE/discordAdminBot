namespace Web;

public record AppConfig(
  string DiscordBotToken,
  string DiscordGuildId,
  string CacheDbPath,
  string AzureCommunicationConnectionString,
  string EmailSenderAddress
)
{
  public static AppConfig FromConfiguration(IConfiguration config) =>
    new(
      DiscordBotToken: config["DISCORD_BOT_TOKEN"]
        ?? throw new InvalidOperationException("DISCORD_BOT_TOKEN is not configured."),
      DiscordGuildId: config["DISCORD_GUILD_ID"]
        ?? throw new InvalidOperationException("DISCORD_GUILD_ID is not configured."),
      CacheDbPath: config["CACHE_DB_PATH"] ?? "discord_cache.db",
      AzureCommunicationConnectionString: config["AZURE_COMMUNICATION_CONNECTION_STRING"]
        ?? throw new InvalidOperationException(
          "AZURE_COMMUNICATION_CONNECTION_STRING is not configured."
        ),
      EmailSenderAddress: config["EMAIL_SENDER_ADDRESS"]
        ?? throw new InvalidOperationException("EMAIL_SENDER_ADDRESS is not configured.")
    );
}
