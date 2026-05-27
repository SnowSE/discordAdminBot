using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordMemberId>))]
public sealed record DiscordMemberId(string value) : TypedString(value)
{
  public static implicit operator DiscordMemberId(string value) => new(value);
}
