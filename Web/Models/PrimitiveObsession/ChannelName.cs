using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedStringJsonConverter<ChannelName>))]
public sealed record ChannelName(string value) : TypedString(value)
{
  public static implicit operator ChannelName(string value) => new(value);
}
