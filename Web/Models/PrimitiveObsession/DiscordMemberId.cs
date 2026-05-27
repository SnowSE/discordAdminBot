using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<DiscordMemberId>))]
public sealed record DiscordMemberId(string Value) : TypedString(Value)
{
  public static implicit operator DiscordMemberId(string value) => new(value);
}
