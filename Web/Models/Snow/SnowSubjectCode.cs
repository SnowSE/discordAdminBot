using System.Text.Json.Serialization;

namespace Web.Models.Snow;

[JsonConverter(typeof(TypedStringJsonConverter<SnowSubjectCode>))]
public sealed record SnowSubjectCode(string value) : TypedString(value)
{
  public static implicit operator SnowSubjectCode(string value) => new(value);
}
