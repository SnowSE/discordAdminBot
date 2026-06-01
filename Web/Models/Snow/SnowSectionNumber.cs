using System.Text.Json.Serialization;

namespace Web.Models.Snow;

[JsonConverter(typeof(TypedStringJsonConverter<SnowSectionNumber>))]
public sealed record SnowSectionNumber(string value) : TypedString(value)
{
  public static implicit operator SnowSectionNumber(string value) => new(value);
}
