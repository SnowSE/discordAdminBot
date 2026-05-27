using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordRoleId>))]
public sealed record DiscordRoleId(string value) : TypedString(value)
{
  public static implicit operator DiscordRoleId(string value) => new(value);
}
