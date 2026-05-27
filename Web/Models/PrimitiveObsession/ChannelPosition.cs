using System.Text.Json.Serialization;

namespace Web.Models;

[JsonConverter(typeof(TypedIntJsonConverter<ChannelPosition>))]
public sealed record ChannelPosition(int value) : TypedInt(value)
{
  public static implicit operator ChannelPosition(int value) => new(value);
}
