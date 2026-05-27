using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<GuildJoinedAt>))]
public sealed record GuildJoinedAt(string value) : TypedString(value)
{
  public static implicit operator GuildJoinedAt(string value) => new(value);
}
