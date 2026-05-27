using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordUsername>))]
public sealed record DiscordUsername(string value) : TypedString(value)
{
  public static implicit operator DiscordUsername(string value) => new(value);
}
