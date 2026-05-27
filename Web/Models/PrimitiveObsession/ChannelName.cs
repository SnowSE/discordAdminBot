using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<ChannelName>))]
public sealed record ChannelName(string Value) : TypedString(Value)
{
  public static implicit operator ChannelName(string value) => new(value);
}
