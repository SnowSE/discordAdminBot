using System.Text.Json.Serialization;

namespace Web.Models.Snow;

[JsonConverter(typeof(TypedStringJsonConverter<SnowCourseNumber>))]
public sealed record SnowCourseNumber(string value) : TypedString(value)
{
  public static implicit operator SnowCourseNumber(string value) => new(value);
}
