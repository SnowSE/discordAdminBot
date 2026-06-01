using System.Text.Json.Serialization;

namespace Web.Models.Snow;

[JsonConverter(typeof(TypedStringJsonConverter<SnowTermCode>))]
public sealed record SnowTermCode(string value) : TypedString(value)
{
  public static implicit operator SnowTermCode(string value) => new(value);
}
