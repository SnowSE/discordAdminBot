using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<GuildNick>))]
public sealed record GuildNick(string value) : TypedString(value)
{
  public static implicit operator GuildNick(string value) => new(value);
}
