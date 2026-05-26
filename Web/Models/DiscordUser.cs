namespace Web.Models;

public record DiscordUser(string Id, string Username, string? GlobalName, string? Avatar, bool Bot);
