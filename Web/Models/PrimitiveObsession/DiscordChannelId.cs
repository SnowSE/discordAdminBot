using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordChannelId>))]
public sealed record DiscordChannelId(string Value) : TypedString(Value)
{
  public static implicit operator DiscordChannelId(string value) => new(value);
}
