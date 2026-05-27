using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordGuildId>))]
public sealed record DiscordGuildId(string Value) : TypedString(Value)
{
  public static implicit operator DiscordGuildId(string value) => new(value);
}
