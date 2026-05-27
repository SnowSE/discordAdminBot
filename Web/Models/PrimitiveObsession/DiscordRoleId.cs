using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordRoleId>))]
public sealed record DiscordRoleId(string Value) : TypedString(Value)
{
  public static implicit operator DiscordRoleId(string value) => new(value);
}
