using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordGlobalName>))]
public sealed record DiscordGlobalName(string value) : TypedString(value)
{
  public static implicit operator DiscordGlobalName(string value) => new(value);
}
