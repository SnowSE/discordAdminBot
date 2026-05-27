using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordChannelId>))]
public sealed record DiscordChannelId(string value) : TypedString(value)
{
  public static implicit operator DiscordChannelId(string value) => new(value);
}
