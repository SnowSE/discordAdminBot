using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordUserId>))]
public sealed record DiscordUserId(string Value) : TypedString(Value)
{
  public static implicit operator DiscordUserId(string value) => new(value);
}
