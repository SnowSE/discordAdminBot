using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordAvatar>))]
public sealed record DiscordAvatar(string value) : TypedString(value)
{
  public static implicit operator DiscordAvatar(string value) => new(value);
}
